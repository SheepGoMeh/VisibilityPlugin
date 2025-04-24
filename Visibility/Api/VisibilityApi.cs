using System;
using System.Collections.Generic;
using System.Linq;

using Lumina.Excel.Sheets;

using Visibility.Void;

namespace Visibility.Api;

public class VisibilityApi: IDisposable, IVisibilityApi
{
	public int ApiVersion => 1;

	private bool initialised;

	public VisibilityApi() => this.initialised = true;

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

		return VisibilityPlugin
			.Instance
			.Configuration
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

		VisibilityPlugin.Instance.CommandManagerHandler.VoidPlayer("", $"{name} {world.Value.Name.ToString()} {reason}");
	}

	public void RemoveFromVoidList(string name, uint worldId)
	{
		this.CheckInitialised();

		VoidItem? item = VisibilityPlugin.Instance.Configuration.VoidList.SingleOrDefault(
			x => x.Name == name && x.HomeworldId == worldId);

		if (item == null)
		{
			return;
		}

		VisibilityPlugin.Instance.Configuration.VoidList.Remove(item);
		VisibilityPlugin.Instance.Configuration.Save();

		if (item.ObjectId > 0)
		{
			VisibilityPlugin.Instance.RemoveChecked(item.ObjectId);
			VisibilityPlugin.Instance.ShowPlayer(item.ObjectId);
		}
		else
		{
			VisibilityPlugin.Instance.RemoveChecked(item.Name);
		}
	}

	public IEnumerable<string> GetWhitelistEntries()
	{
		this.CheckInitialised();

		return VisibilityPlugin
			.Instance
			.Configuration
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

		VisibilityPlugin.Instance.CommandManagerHandler.WhitelistPlayer("", $"{name} {world.Value.Name.ToString()} {reason}");
	}

	public void RemoveFromWhitelist(string name, uint worldId)
	{
		this.CheckInitialised();

		VoidItem? item = VisibilityPlugin.Instance.Configuration.Whitelist.SingleOrDefault(
			x => x.Name == name && x.HomeworldId == worldId);

		if (item == null)
		{
			return;
		}

		VisibilityPlugin.Instance.Configuration.Whitelist.Remove(item);
		VisibilityPlugin.Instance.Configuration.Save();
	}

	public void Enable(bool state) =>
		VisibilityPlugin.Instance.Configuration.SettingsHandler
			.Invoke(nameof(VisibilityPlugin.Instance.Configuration.Enabled), state, false, false);

	public void Dispose() => this.initialised = false;
}
