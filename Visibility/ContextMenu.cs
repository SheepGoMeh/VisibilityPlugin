using System;
using System.Linq;
using System.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.GeneratedSheets;
using XivCommon.Functions.ContextMenu;

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
				Plugin.Common.Functions.ContextMenu.OpenContextMenu -= OnOpenContextMenu;
			}
			else
			{
				Plugin.Common.Functions.ContextMenu.OpenContextMenu += OnOpenContextMenu;
			}

			_enabled = !_enabled;
		}

		public void Dispose()
		{
			Plugin.Common.Functions.ContextMenu.OpenContextMenu -= OnOpenContextMenu;
		}
		
		private void OnOpenContextMenu(ContextMenuOpenArgs args) {
			if (args.ParentAddonName is not "ChatLog" || args.ContentIdLower == 0)
			{
				return;
			}

			args.Items.Add(Plugin.PluginConfiguration.VoidList.SingleOrDefault(x =>
				x.Name == args.Text && x.HomeworldId == args.ActorWorld) == null
				? new NormalContextMenuItem("Add to VoidList", AddToVoidList)
				: new NormalContextMenuItem("Remove from VoidList", RemoveFromVoidList));

			args.Items.Add(Plugin.PluginConfiguration.Whitelist.SingleOrDefault(x =>
				x.Name == args.Text && x.HomeworldId == args.ActorWorld) == null
				? new NormalContextMenuItem("Add to Whitelist", AddToWhitelist)
				: new NormalContextMenuItem("Remove from Whitelist", RemoveFromWhitelist));
		}

		private void AddToVoidList(ContextMenuItemSelectedArgs args)
		{
			var world = Plugin.PluginInterface.Data.GetExcelSheet<World>()
				.SingleOrDefault(x => x.RowId == args.ActorWorld);

			if (world == null)
			{
				return;
			}
			
			Plugin.VoidPlayer("ContextMenu", $"{args.Text} {world.Name}");
		}
		
		private void RemoveFromVoidList(ContextMenuItemSelectedArgs args)
		{
			var entry = Plugin.PluginConfiguration.VoidList.SingleOrDefault(x =>
				x.Name == args.Text && x.HomeworldId == args.ActorWorld);

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

			Plugin.PluginConfiguration.VoidList.Remove(entry);
			Plugin.Print(message);
		}
		
		private void AddToWhitelist(ContextMenuItemSelectedArgs args)
		{
			var world = Plugin.PluginInterface.Data.GetExcelSheet<World>()
				.SingleOrDefault(x => x.RowId == args.ActorWorld);

			if (world == null)
			{
				return;
			}
			
			Plugin.WhitelistPlayer("ContextMenu", $"{args.Text} {world.Name}");
		}
		
		private void RemoveFromWhitelist(ContextMenuItemSelectedArgs args)
		{
			var entry = Plugin.PluginConfiguration.Whitelist.SingleOrDefault(x =>
				x.Name == args.Text && x.HomeworldId == args.ActorWorld);

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

			Plugin.PluginConfiguration.Whitelist.Remove(entry);
			Plugin.Print(message);
		}
	}
}