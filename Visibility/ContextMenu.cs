using System;
using System.Linq;
using System.Text;
using Dalamud.Game.Gui.ContextMenus;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.GeneratedSheets;

namespace Visibility
{
	public class ContextMenu : IDisposable
	{
		private bool _enabled;
		private VisibilityPlugin Plugin { get; }

		public ContextMenu(VisibilityPlugin plugin)
		{
			Plugin = plugin;
		}

		public void Toggle()
		{
			if (_enabled)
			{
				Plugin.ContextMenu.ContextMenuOpened -= OnOpenContextMenu;
			}
			else
			{
				Plugin.ContextMenu.ContextMenuOpened += OnOpenContextMenu;
			}

			_enabled = !_enabled;
		}

		public void Dispose()
		{
			Plugin.ContextMenu.ContextMenuOpened -= OnOpenContextMenu;
		}
		
		private void OnOpenContextMenu(ContextMenuOpenedArgs args) {
			if (args.ParentAddonName is not "ChatLog")
			{
				return;
			}

			if (args.GameObjectContext?.WorldId is ushort.MaxValue or 0 || args.GameObjectContext?.Name is null)
			{
				return;
			}

			if (Plugin.Configuration.VoidList.SingleOrDefault(x =>
				    x.Name == args.GameObjectContext?.Name && x.HomeworldId == args.GameObjectContext?.WorldId) == null)
			{
				args.AddCustomItem(Plugin.PluginLocalization.ContextMenuAdd(Plugin.PluginLocalization.VoidListName),
					AddToVoidList);
			}
			else
			{
				args.AddCustomItem(Plugin.PluginLocalization.ContextMenuRemove(Plugin.PluginLocalization.VoidListName),
					RemoveFromVoidList);
			}
			
			if (Plugin.Configuration.Whitelist.SingleOrDefault(x =>
				    x.Name == args.GameObjectContext?.Name && x.HomeworldId == args.GameObjectContext?.WorldId) == null)
			{
				args.AddCustomItem(Plugin.PluginLocalization.ContextMenuAdd(Plugin.PluginLocalization.WhitelistName),
					AddToWhitelist);
			}
			else
			{
				args.AddCustomItem(Plugin.PluginLocalization.ContextMenuRemove(Plugin.PluginLocalization.WhitelistName),
					RemoveFromWhitelist);
			}
		}

		private void AddToVoidList(CustomContextMenuItemSelectedArgs args)
		{
			var world = Plugin.DataManager.GetExcelSheet<World>()?
				.SingleOrDefault(x => x.RowId == args.ContextMenuOpenedArgs.GameObjectContext?.WorldId);

			if (world == null)
			{
				return;
			}
			
			Plugin.VoidPlayer("ContextMenu", $"{args.ContextMenuOpenedArgs.GameObjectContext?.Name} {world.Name}");
		}
		
		private void RemoveFromVoidList(CustomContextMenuItemSelectedArgs args)
		{
			var entry = Plugin.Configuration.VoidList.SingleOrDefault(x =>
				x.Name == args.ContextMenuOpenedArgs.GameObjectContext?.Name && x.HomeworldId == args.ContextMenuOpenedArgs.GameObjectContext?.WorldId);

			if (entry == null)
			{
				return;
			}

			var message = Encoding.UTF8.GetString(new SeString(new Payload[]
			{
				new TextPayload("VoidList: " + entry.Name),
				new IconPayload(BitmapFontIcon.CrossWorld),
				new TextPayload(entry.HomeworldName + " has been removed."),
			}).Encode());

			Plugin.Configuration.VoidList.Remove(entry);
			Plugin.Configuration.Save();
			Plugin.ShowPlayer(args.ContextMenuOpenedArgs.GameObjectContext!.Id!.Value);
			Plugin.ChatGui.Print(message);
		}
		
		private void AddToWhitelist(CustomContextMenuItemSelectedArgs args)
		{
			var world = Plugin.DataManager.GetExcelSheet<World>()?
				.SingleOrDefault(x => x.RowId == args.ContextMenuOpenedArgs.GameObjectContext?.WorldId);

			if (world == null)
			{
				return;
			}
			
			Plugin.WhitelistPlayer("ContextMenu", $"{args.ContextMenuOpenedArgs.GameObjectContext?.Name} {world.Name}");
		}
		
		private void RemoveFromWhitelist(CustomContextMenuItemSelectedArgs args)
		{
			var entry = Plugin.Configuration.Whitelist.SingleOrDefault(x =>
				x.Name == args.ContextMenuOpenedArgs.GameObjectContext?.Name && x.HomeworldId == args.ContextMenuOpenedArgs.GameObjectContext?.WorldId);

			if (entry == null)
			{
				return;
			}

			var message = Encoding.UTF8.GetString(new SeString(new Payload[]
			{
				new TextPayload("Whitelist: " + entry.Name),
				new IconPayload(BitmapFontIcon.CrossWorld),
				new TextPayload(entry.HomeworldName + " has been removed."),
			}).Encode());

			Plugin.Configuration.Whitelist.Remove(entry);
			Plugin.Configuration.Save();
			Plugin.ChatGui.Print(message);
		}
	}
}