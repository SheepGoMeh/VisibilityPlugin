using System;
using System.Linq;
using System.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
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
			if (args.ParentAddonName is not "ChatLog")
			{
				return;
			}

			if (args.ObjectWorld is ushort.MaxValue or 0 || args.Text?.Payloads.Count != 1 ||
			    args.Text?.Payloads[0] is not TextPayload textPayload)
			{
				return;
			}

			args.Items.Add(Plugin.Configuration.VoidList.SingleOrDefault(x =>
				x.Name == textPayload.Text && x.HomeworldId == args.ObjectWorld) == null
				? new NormalContextMenuItem(
					Plugin.PluginLocalization.ContextMenuAdd(Plugin.PluginLocalization.VoidListName), AddToVoidList)
				: new NormalContextMenuItem(
					Plugin.PluginLocalization.ContextMenuRemove(Plugin.PluginLocalization.VoidListName),
					RemoveFromVoidList));

			args.Items.Add(Plugin.Configuration.Whitelist.SingleOrDefault(x =>
				x.Name == textPayload.Text && x.HomeworldId == args.ObjectWorld) == null
				? new NormalContextMenuItem(
					Plugin.PluginLocalization.ContextMenuAdd(Plugin.PluginLocalization.WhitelistName), AddToWhitelist)
				: new NormalContextMenuItem(
					Plugin.PluginLocalization.ContextMenuRemove(Plugin.PluginLocalization.WhitelistName),
					RemoveFromWhitelist));
		}

		private void AddToVoidList(ContextMenuItemSelectedArgs args)
		{
			var world = Plugin.DataManager.GetExcelSheet<World>()
				.SingleOrDefault(x => x.RowId == args.ObjectWorld);

			if (world == null)
			{
				return;
			}
			
			Plugin.VoidPlayer("ContextMenu", $"{args.Text} {world.Name}");
		}
		
		private void RemoveFromVoidList(ContextMenuItemSelectedArgs args)
		{
			var entry = Plugin.Configuration.VoidList.SingleOrDefault(x =>
				x.Name == args.Text?.TextValue && x.HomeworldId == args.ObjectWorld);

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
			Plugin.ShowPlayer(args.ObjectId);
			Plugin.ChatGui.Print(message);
		}
		
		private void AddToWhitelist(ContextMenuItemSelectedArgs args)
		{
			var world = Plugin.DataManager.GetExcelSheet<World>()
				.SingleOrDefault(x => x.RowId == args.ObjectWorld);

			if (world == null)
			{
				return;
			}
			
			Plugin.WhitelistPlayer("ContextMenu", $"{args.Text} {world.Name}");
		}
		
		private void RemoveFromWhitelist(ContextMenuItemSelectedArgs args)
		{
			var entry = Plugin.Configuration.Whitelist.SingleOrDefault(x =>
				x.Name == args.Text?.TextValue && x.HomeworldId == args.ObjectWorld);

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