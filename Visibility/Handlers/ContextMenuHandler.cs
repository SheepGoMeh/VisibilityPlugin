using System;
using System.Linq;

using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.ClientState.Objects.SubKinds;

using Lumina.Excel.Sheets;

using Visibility.Configuration;
using Visibility.Utils;
using Visibility.Void;

namespace Visibility.Handlers;

public class ContextMenuHandler: IDisposable
{
	private readonly VisibilityConfiguration configuration;
	private readonly CommandManagerHandler commandManagerHandler;
	private readonly FrameworkHandler frameworkHandler;
	private readonly Localization pluginLocalization;

	public ContextMenuHandler(
		VisibilityConfiguration configuration,
		CommandManagerHandler commandManagerHandler,
		FrameworkHandler frameworkHandler,
		Localization pluginLocalization)
	{
		this.configuration = configuration;
		this.commandManagerHandler = commandManagerHandler;
		this.frameworkHandler = frameworkHandler;
		this.pluginLocalization = pluginLocalization;

		Service.ContextMenu.OnMenuOpened += this.OnMenuOpened;
	}

	private void OnMenuOpened(IMenuOpenedArgs args)
	{
		if (!this.configuration.EnableContextMenu)
		{
			return;
		}

		if (args.Target is not MenuTargetDefault target)
		{
			return;
		}

		if (target.TargetObject is not null and not IPlayerCharacter)
		{
			return;
		}

		uint worldId = target.TargetHomeWorld.RowId;
		if (worldId is 0u or ushort.MaxValue)
		{
			return;
		}

		string targetName = target.TargetName;
		if (string.IsNullOrEmpty(targetName))
		{
			return;
		}

		args.AddMenuItem(new MenuItem
		{
			Name = "Visibility",
			Prefix = SeIconChar.BoxedLetterV,
			PrefixColor = 539,
			IsSubmenu = true,
			OnClicked = clickArgs => this.OpenVisibilitySubmenu(clickArgs, targetName, worldId),
		});
	}

	private void OpenVisibilitySubmenu(IMenuItemClickedArgs args, string targetName, uint worldId)
	{
		VoidItem? voidEntry = this.FindVoidEntry(targetName, worldId);
		VoidItem? whitelistEntry = this.FindWhitelistEntry(targetName, worldId);

		MenuItem voidItem = voidEntry is null
			? new MenuItem
			{
				Name = BuildLabel(this.pluginLocalization.ContextMenuAdd(this.pluginLocalization.VoidListName)),
				OnClicked = _ => this.AddToVoidList(targetName, worldId),
			}
			: new MenuItem
			{
				Name = BuildLabel(this.pluginLocalization.ContextMenuRemove(this.pluginLocalization.VoidListName)),
				OnClicked = _ => this.RemoveFromVoidList(voidEntry),
			};

		MenuItem whitelistItem = whitelistEntry is null
			? new MenuItem
			{
				Name = BuildLabel(this.pluginLocalization.ContextMenuAdd(this.pluginLocalization.WhitelistName)),
				OnClicked = _ => this.AddToWhitelist(targetName, worldId),
			}
			: new MenuItem
			{
				Name = BuildLabel(this.pluginLocalization.ContextMenuRemove(this.pluginLocalization.WhitelistName)),
				OnClicked = _ => this.RemoveFromWhitelist(whitelistEntry),
			};

		args.OpenSubmenu("Visibility", new MenuItem[] { voidItem, whitelistItem });
	}

	private static SeString BuildLabel(string text) =>
		new(
			new UIForegroundPayload(539),
			new TextPayload($"{SeIconChar.BoxedLetterV.ToIconString()} "),
			new UIForegroundPayload(0),
			new TextPayload(text));

	private VoidItem? FindVoidEntry(string name, uint worldId) =>
		this.configuration.VoidList.SingleOrDefault(
			x => x.Name == name && x.HomeworldId == worldId);

	private VoidItem? FindWhitelistEntry(string name, uint worldId) =>
		this.configuration.Whitelist.SingleOrDefault(
			x => x.Name == name && x.HomeworldId == worldId);

	private void AddToVoidList(string name, uint worldId)
	{
		if (!Service.DataManager.GetExcelSheet<World>().TryGetRow(worldId, out World world))
		{
			return;
		}

		this.commandManagerHandler.VoidPlayer("ContextMenu", $"{name} {world.Name}");
	}

	private void RemoveFromVoidList(VoidItem entry)
	{
		SeString message = new(
			new TextPayload("VoidList: "),
			new PlayerPayload(entry.Name, entry.HomeworldId),
			new IconPayload(BitmapFontIcon.CrossWorld),
			new TextPayload($"{entry.HomeworldName} has been removed."));

		this.configuration.VoidList.Remove(entry);
		if (entry.Id != 0)
		{
			this.configuration.VoidDictionary.Remove(entry.Id);
		}
		this.configuration.Save();
		if (entry.ObjectId > 0)
		{
			this.frameworkHandler.RemoveChecked(entry.ObjectId);
			this.frameworkHandler.ShowPlayer(entry.ObjectId);
		}
		else
		{
			this.frameworkHandler.RemoveChecked(entry.Name);
			this.frameworkHandler.ShowPlayer(entry.Name);
		}

		Service.ChatGui.Print(message);
	}

	private void AddToWhitelist(string name, uint worldId)
	{
		if (!Service.DataManager.GetExcelSheet<World>().TryGetRow(worldId, out World world))
		{
			return;
		}

		this.commandManagerHandler.WhitelistPlayer("ContextMenu", $"{name} {world.Name}");
	}

	private void RemoveFromWhitelist(VoidItem entry)
	{
		SeString message = new(
			new TextPayload("Whitelist: "),
			new PlayerPayload(entry.Name, entry.HomeworldId),
			new IconPayload(BitmapFontIcon.CrossWorld),
			new TextPayload($"{entry.HomeworldName} has been removed."));

		this.configuration.Whitelist.Remove(entry);
		if (entry.Id != 0)
		{
			this.configuration.WhitelistDictionary.Remove(entry.Id);
		}
		this.configuration.Save();
		if (entry.ObjectId > 0)
		{
			this.frameworkHandler.RemoveChecked(entry.ObjectId);
		}
		else
		{
			this.frameworkHandler.RemoveChecked(entry.Name);
		}

		Service.ChatGui.Print(message);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		Service.ContextMenu.OnMenuOpened -= this.OnMenuOpened;
	}

	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}
}
