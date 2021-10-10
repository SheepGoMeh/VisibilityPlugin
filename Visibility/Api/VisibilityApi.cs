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

		private bool _initialised;
		private readonly VisibilityPlugin _plugin;


		public VisibilityApi(VisibilityPlugin plugin)
		{
			_plugin = plugin;
			_initialised = true;
		}

		private void CheckInitialised()
		{
			if (!_initialised)
			{
				throw new Exception("PluginShare is not initialised.");
			}
		}

		public IEnumerable<string> GetVoidListEntries()
		{
			CheckInitialised();

			return _plugin
				.Configuration
				.VoidList
				.Select(x => $"{x.Name} {x.HomeworldId} {x.Reason}")
				.ToList();
		}

		public void AddToVoidList(string name, uint worldId, string reason)
		{
			CheckInitialised();

			var world = _plugin.DataManager.GetExcelSheet<World>()?.SingleOrDefault(x => x.RowId == worldId);

			if (world == null)
			{
				throw new Exception($"Invalid worldId ({worldId}).");
			}

			_plugin.VoidPlayer("", $"{name} {world.Name} {reason}");
		}

		public void RemoveFromVoidList(string name, uint worldId)
		{
			CheckInitialised();

			var item = _plugin.Configuration.VoidList.SingleOrDefault(x => x.Name == name && x.HomeworldId == worldId);

			if (item == null)
			{
				return;
			}

			if (_plugin.ObjectTable
				.SingleOrDefault(x =>
					x is PlayerCharacter playerCharacter &&
					playerCharacter.Name.TextValue.Equals(item.Name, StringComparison.Ordinal) &&
					playerCharacter.HomeWorld.Id == item.HomeworldId) is PlayerCharacter a)
			{
				a.Render();
			}

			_plugin.Configuration.VoidList.Remove(item);
			_plugin.Configuration.Save();
		}

		public void Dispose()
		{
			_initialised = false;
		}
	}
}