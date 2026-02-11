using System;
using System.Collections.Generic;
using System.Linq;

using Lumina.Excel.Sheets;

using Visibility.Configuration;
using Visibility.Handlers;
using Visibility.Utils;
using Visibility.Void;

namespace Visibility.Api;

public class VisibilityApi: IDisposable, IVisibilityApi
{
	public int ApiVersion => 1;

	private readonly VisibilityConfiguration configuration;
	private readonly CommandManagerHandler commandManagerHandler;
	private readonly FrameworkHandler frameworkHandler;
	private bool initialised;

	public VisibilityApi(
		VisibilityConfiguration configuration,
		CommandManagerHandler commandManagerHandler,
		FrameworkHandler frameworkHandler)
	{
		this.configuration = configuration;
		this.commandManagerHandler = commandManagerHandler;
		this.frameworkHandler = frameworkHandler;
		this.initialised = true;
	}

	private void CheckInitialised()
	{
		if (!this.initialised)
		{
			throw new Exception("PluginShare is not initialised.");
		}
	}

	public IEnumerable<string> GetVoidListEntries()
	{
		this.CheckInitialised();

		return this.configuration
			.VoidList
			.Select(x => $"{x.Name} {x.HomeworldId} {x.Reason}")
			.ToList();
	}

	public void AddToVoidList(string name, uint worldId, string reason)
	{
		this.CheckInitialised();

		World? world = Service.DataManager.GetExcelSheet<World>().SingleOrDefault(x => x.RowId == worldId);

		if (world == null)
		{
			throw new Exception($"Invalid worldId ({worldId}).");
		}

		this.commandManagerHandler.VoidPlayer("", $"{name} {world.Value.Name.ToString()} {reason}");
	}

	public void RemoveFromVoidList(string name, uint worldId)
	{
		this.CheckInitialised();

		VoidItem? item = this.configuration.VoidList.SingleOrDefault(
			x => x.Name == name && x.HomeworldId == worldId);

		if (item == null)
		{
			return;
		}

		this.configuration.VoidList.Remove(item);
		this.configuration.Save();

		if (item.ObjectId > 0)
		{
			this.frameworkHandler.RemoveChecked(item.ObjectId);
			this.frameworkHandler.ShowPlayer(item.ObjectId);
		}
		else
		{
			this.frameworkHandler.RemoveChecked(item.Name);
		}
	}

	public IEnumerable<string> GetWhitelistEntries()
	{
		this.CheckInitialised();

		return this.configuration
			.Whitelist
			.Select(x => $"{x.Name} {x.HomeworldId} {x.Reason}")
			.ToList();
	}

	public void AddToWhitelist(string name, uint worldId, string reason)
	{
		this.CheckInitialised();

		World? world = Service.DataManager.GetExcelSheet<World>().SingleOrDefault(x => x.RowId == worldId);

		if (world == null)
		{
			throw new Exception($"Invalid worldId ({worldId}).");
		}

		this.commandManagerHandler.WhitelistPlayer("", $"{name} {world.Value.Name.ToString()} {reason}");
	}

	public void RemoveFromWhitelist(string name, uint worldId)
	{
		this.CheckInitialised();

		VoidItem? item = this.configuration.Whitelist.SingleOrDefault(
			x => x.Name == name && x.HomeworldId == worldId);

		if (item == null)
		{
			return;
		}

		this.configuration.Whitelist.Remove(item);
		this.configuration.Save();
	}

	public void Enable(bool state) =>
		this.configuration.SettingsHandler
			.Invoke(nameof(this.configuration.Enabled), state, false, false);

	public bool IsEnable() => this.configuration.Enabled;

	public void Dispose() => this.initialised = false;
}
