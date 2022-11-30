using System;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.GeneratedSheets;
using Dalamud.ContextMenu;
using Dalamud.Game.Text;

namespace Visibility
{
	public class ContextMenu : IDisposable
	{
		private bool enabled;
		private readonly DalamudContextMenu dalamudContextMenu;

		public ContextMenu()
		{
			this.dalamudContextMenu = new DalamudContextMenu();
		}

		public void Toggle()
		{
			if (this.enabled)
			{
				this.dalamudContextMenu.OnOpenGameObjectContextMenu -= OnOpenContextMenu;
			}
			else
			{
				this.dalamudContextMenu.OnOpenGameObjectContextMenu += OnOpenContextMenu;
			}

			this.enabled = !this.enabled;
		}

		public void Toggle(bool value, bool toggle)
		{
			if (this.enabled != value || toggle)
			{
				this.Toggle();
			}
		}

		private void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			this.dalamudContextMenu.OnOpenGameObjectContextMenu -= OnOpenContextMenu;
			this.dalamudContextMenu.Dispose();
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		private static void OnOpenContextMenu(GameObjectContextMenuOpenArgs args)
		{
			if (args.ParentAddonName is not "ChatLog")
			{
				return;
			}

			if (args.ObjectWorld is ushort.MaxValue or 0 || args.Text?.Payloads.Count != 1 ||
			    args.Text?.Payloads[0] is not TextPayload textPayload)
			{
				return;
			}

			args.AddCustomItem(
				VisibilityPlugin.Instance.Configuration.VoidList.SingleOrDefault(
					x => x.Name == textPayload.Text && x.HomeworldId == args.ObjectWorld) == null
					? new GameObjectContextMenuItem(
						new SeString(
							new UIForegroundPayload(539),
							new TextPayload($"{SeIconChar.BoxedLetterV.ToIconString()} "),
							new UIForegroundPayload(0),
							new TextPayload(
								VisibilityPlugin.Instance.PluginLocalization.ContextMenuAdd(
									VisibilityPlugin.Instance.PluginLocalization.VoidListName))),
						AddToVoidList)
					: new GameObjectContextMenuItem(
						new SeString(
							new UIForegroundPayload(539),
							new TextPayload($"{SeIconChar.BoxedLetterV.ToIconString()} "),
							new UIForegroundPayload(0),
							new TextPayload(
								VisibilityPlugin.Instance.PluginLocalization.ContextMenuRemove(
									VisibilityPlugin.Instance.PluginLocalization.VoidListName))),
						RemoveFromVoidList));

			args.AddCustomItem(
				VisibilityPlugin.Instance.Configuration.Whitelist.SingleOrDefault(
					x => x.Name == textPayload.Text && x.HomeworldId == args.ObjectWorld) == null
					? new GameObjectContextMenuItem(
						new SeString(
							new UIForegroundPayload(539),
							new TextPayload($"{SeIconChar.BoxedLetterV.ToIconString()} "),
							new UIForegroundPayload(0),
							new TextPayload(
								VisibilityPlugin.Instance.PluginLocalization.ContextMenuAdd(
									VisibilityPlugin.Instance.PluginLocalization.WhitelistName))),
						AddToWhitelist)
					: new GameObjectContextMenuItem(
						new SeString(
							new UIForegroundPayload(539),
							new TextPayload($"{SeIconChar.BoxedLetterV.ToIconString()} "),
							new UIForegroundPayload(0),
							new TextPayload(
								VisibilityPlugin.Instance.PluginLocalization.ContextMenuRemove(
									VisibilityPlugin.Instance.PluginLocalization.WhitelistName))),
						RemoveFromWhitelist));
		}

		private static void AddToVoidList(GameObjectContextMenuItemSelectedArgs args)
		{
			var world = VisibilityPlugin.DataManager.GetExcelSheet<World>()?
				.SingleOrDefault(x => x.RowId == args.ObjectWorld);

			if (world == null)
			{
				return;
			}

			VisibilityPlugin.Instance.VoidPlayer("ContextMenu", $"{args.Text} {world.Name}");
		}

		private static void RemoveFromVoidList(GameObjectContextMenuItemSelectedArgs args)
		{
			var entry = VisibilityPlugin.Instance.Configuration.VoidList.SingleOrDefault(
				x =>
					x.Name == args.Text?.TextValue && x.HomeworldId == args.ObjectWorld);

			if (entry == null)
			{
				return;
			}

			var message = new SeString(
				new TextPayload("VoidList: "),
				new PlayerPayload(entry.Name, entry.HomeworldId),
				new IconPayload(BitmapFontIcon.CrossWorld),
				new TextPayload($"{entry.HomeworldName} has been removed."));

			VisibilityPlugin.Instance.Configuration.VoidList.Remove(entry);
			VisibilityPlugin.Instance.Configuration.Save();
			VisibilityPlugin.Instance.ShowPlayer(args.ObjectId);
			VisibilityPlugin.ChatGui.Print(message);
		}

		private static void AddToWhitelist(GameObjectContextMenuItemSelectedArgs args)
		{
			var world = VisibilityPlugin.DataManager.GetExcelSheet<World>()?
				.SingleOrDefault(x => x.RowId == args.ObjectWorld);

			if (world == null)
			{
				return;
			}

			VisibilityPlugin.Instance.WhitelistPlayer("ContextMenu", $"{args.Text} {world.Name}");
		}

		private static void RemoveFromWhitelist(GameObjectContextMenuItemSelectedArgs args)
		{
			var entry = VisibilityPlugin.Instance.Configuration.Whitelist.SingleOrDefault(
				x =>
					x.Name == args.Text?.TextValue && x.HomeworldId == args.ObjectWorld);

			if (entry == null)
			{
				return;
			}

			var message = new SeString(
				new TextPayload("Whitelist: "),
				new PlayerPayload(entry.Name, entry.HomeworldId),
				new IconPayload(BitmapFontIcon.CrossWorld),
				new TextPayload($"{entry.HomeworldName} has been removed."));

			VisibilityPlugin.Instance.Configuration.Whitelist.Remove(entry);
			VisibilityPlugin.Instance.Configuration.Save();
			VisibilityPlugin.ChatGui.Print(message);
		}
	}
}