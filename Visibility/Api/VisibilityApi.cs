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

		public VisibilityApi()
		{
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

			var world = VisibilityPlugin.DataManager.GetExcelSheet<World>()?.SingleOrDefault(x => x.RowId == worldId);

			if (world == null)
			{
				throw new Exception($"Invalid worldId ({worldId}).");
			}

			VisibilityPlugin.Instance.VoidPlayer("", $"{name} {world.Name} {reason}");
		}

		public void RemoveFromVoidList(string name, uint worldId)
		{
			this.CheckInitialised();

			var item = VisibilityPlugin.Instance.Configuration.VoidList.SingleOrDefault(
				x => x.Name == name && x.HomeworldId == worldId);

			if (item == null)
			{
				return;
			}

			if (VisibilityPlugin.ObjectTable
				    .SingleOrDefault(
					    x =>
						    x is PlayerCharacter playerCharacter &&
						    playerCharacter.Name.TextValue.Equals(item.Name, StringComparison.Ordinal) &&
						    playerCharacter.HomeWorld.Id == item.HomeworldId) is PlayerCharacter a)
			{
				a.Render();
			}

			VisibilityPlugin.Instance.Configuration.VoidList.Remove(item);
			VisibilityPlugin.Instance.Configuration.Save();
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

			var world = VisibilityPlugin.DataManager.GetExcelSheet<World>()?.SingleOrDefault(x => x.RowId == worldId);

			if (world == null)
			{
				throw new Exception($"Invalid worldId ({worldId}).");
			}

			VisibilityPlugin.Instance.WhitelistPlayer("", $"{name} {world.Name} {reason}");
		}

		public void RemoveFromWhitelist(string name, uint worldId)
		{
			this.CheckInitialised();

			var item = VisibilityPlugin.Instance.Configuration.Whitelist.SingleOrDefault(
				x => x.Name == name && x.HomeworldId == worldId);

			if (item == null)
			{
				return;
			}

			VisibilityPlugin.Instance.Configuration.Whitelist.Remove(item);
			VisibilityPlugin.Instance.Configuration.Save();
		}

		public void Enable(bool state)
		{
			VisibilityPlugin.Instance.Configuration
				.SettingDictionary[nameof(VisibilityPlugin.Instance.Configuration.Enabled)].Invoke(state, false, false);
		}

		public void Dispose()
		{
			this.initialised = false;
		}
	}
}