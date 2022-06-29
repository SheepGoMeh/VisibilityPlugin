using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Lumina.Excel.GeneratedSheets;

namespace Visibility.Api
{
	public class VisibilityApi : IDisposable, IVisibilityApi
	{
		public int ApiVersion => 1;

		private bool initialised;
		private readonly VisibilityPlugin plugin;

		public VisibilityApi(VisibilityPlugin plugin)
		{
			this.plugin = plugin;
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

			return this.plugin
				.Configuration
				.VoidList
				.Select(x => $"{x.Name} {x.HomeworldId} {x.Reason}")
				.ToList();
		}

		public void AddToVoidList(string name, uint worldId, string reason)
		{
			this.CheckInitialised();

			var world = this.plugin.DataManager.GetExcelSheet<World>()?.SingleOrDefault(x => x.RowId == worldId);

			if (world == null)
			{
				throw new Exception($"Invalid worldId ({worldId}).");
			}

			this.plugin.VoidPlayer("", $"{name} {world.Name} {reason}");
		}

		public void RemoveFromVoidList(string name, uint worldId)
		{
			this.CheckInitialised();

			var item = this.plugin.Configuration.VoidList.SingleOrDefault(
				x => x.Name == name && x.HomeworldId == worldId);

			if (item == null)
			{
				return;
			}

			if (this.plugin.ObjectTable
				    .SingleOrDefault(
					    x =>
						    x is PlayerCharacter playerCharacter &&
						    playerCharacter.Name.TextValue.Equals(item.Name, StringComparison.Ordinal) &&
						    playerCharacter.HomeWorld.Id == item.HomeworldId) is PlayerCharacter a)
			{
				a.Render();
			}

			this.plugin.Configuration.VoidList.Remove(item);
			this.plugin.Configuration.Save();
		}

		public IEnumerable<string> GetWhitelistEntries()
		{
			this.CheckInitialised();

			return this.plugin
				.Configuration
				.Whitelist
				.Select(x => $"{x.Name} {x.HomeworldId} {x.Reason}")
				.ToList();
		}

		public void AddToWhitelist(string name, uint worldId, string reason)
		{
			this.CheckInitialised();

			var world = this.plugin.DataManager.GetExcelSheet<World>()?.SingleOrDefault(x => x.RowId == worldId);

			if (world == null)
			{
				throw new Exception($"Invalid worldId ({worldId}).");
			}

			this.plugin.WhitelistPlayer("", $"{name} {world.Name} {reason}");
		}

		public void RemoveFromWhitelist(string name, uint worldId)
		{
			this.CheckInitialised();

			var item = this.plugin.Configuration.Whitelist.SingleOrDefault(
				x => x.Name == name && x.HomeworldId == worldId);

			if (item == null)
			{
				return;
			}

			this.plugin.Configuration.Whitelist.Remove(item);
			this.plugin.Configuration.Save();
		}

		public void Enable(bool state)
		{
			this.plugin.Configuration.SettingDictionary["enabled"].Invoke(state ? 1 : 0);
		}

		public void Dispose()
		{
			this.initialised = false;
		}
	}
}