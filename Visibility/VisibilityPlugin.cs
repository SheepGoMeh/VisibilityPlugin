using System;
using System.Linq;
using System.Threading.Tasks;

using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Lumina.Excel.GeneratedSheets;

using Visibility.Api;
using Visibility.Configuration;
using Visibility.Ipc;
using Visibility.Utils;
using Visibility.Void;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace Visibility;

public class VisibilityPlugin: IDalamudPlugin
{
	public string Name => "Visibility";

	private static string PluginCommandName => "/pvis";

	private static string VoidCommandName => "/void";

	private static string VoidTargetCommandName => "/voidtarget";

	private static string WhitelistCommandName => "/whitelist";

	private static string WhitelistTargetCommandName => "/whitelisttarget";

	public readonly Localization PluginLocalization;
	public readonly VisibilityConfiguration Configuration;

	// TODO: Switch to dalamud service
	// public readonly ContextMenu ContextMenu;

	public static VisibilityPlugin Instance { get; private set; } = null!;

	private bool refresh;
	public bool Disable;
	private readonly FrameworkHandler frameworkHandler;

	public VisibilityApi Api { get; }

	public VisibilityProvider IpcProvider { get; }

	public WindowSystem WindowSystem { get; }

	public Windows.Configuration ConfigurationWindow { get; }

	public VisibilityPlugin(IDalamudPluginInterface pluginInterface)
	{
		Instance = this;

		pluginInterface.Create<Service>();
		this.Configuration = Service.PluginInterface.GetPluginConfig() as VisibilityConfiguration ??
		                     new VisibilityConfiguration();
		this.Configuration.Init(Service.ClientState.TerritoryType);
		this.PluginLocalization = new Localization(this.Configuration.Language);

		// TODO: Switch to dalamud service
		// this.ContextMenu = new ContextMenu();
		//
		// if (this.Configuration.EnableContextMenu)
		// {
		// 	this.ContextMenu.Toggle();
		// }

		Service.CommandManager.AddHandler(
			PluginCommandName,
			new CommandInfo(this.PluginCommand)
			{
				HelpMessage = this.PluginLocalization.PluginCommandHelpMessage, ShowInHelp = true
			});

		Service.CommandManager.AddHandler(
			VoidCommandName,
			new CommandInfo(this.VoidPlayer)
			{
				HelpMessage = this.PluginLocalization.VoidPlayerHelpMessage, ShowInHelp = true
			});

		Service.CommandManager.AddHandler(
			VoidTargetCommandName,
			new CommandInfo(this.VoidTargetPlayer)
			{
				HelpMessage = this.PluginLocalization.VoidTargetPlayerHelpMessage, ShowInHelp = true
			});

		Service.CommandManager.AddHandler(
			WhitelistCommandName,
			new CommandInfo(this.WhitelistPlayer)
			{
				HelpMessage = this.PluginLocalization.WhitelistPlayerHelpMessage, ShowInHelp = true
			});

		Service.CommandManager.AddHandler(
			WhitelistTargetCommandName,
			new CommandInfo(this.WhitelistTargetPlayer)
			{
				HelpMessage = this.PluginLocalization.WhitelistTargetPlayerHelpMessage, ShowInHelp = true
			});

		this.frameworkHandler = new FrameworkHandler();

		Service.Framework.Update += this.FrameworkOnOnUpdateEvent;

		this.WindowSystem = new WindowSystem("VisibilityPlugin");
		this.ConfigurationWindow = new Windows.Configuration();
		this.WindowSystem.AddWindow(this.ConfigurationWindow);

		Service.PluginInterface.UiBuilder.Draw += this.BuildUi;
		Service.PluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUi;
		Service.ChatGui.ChatMessage += this.OnChatMessage;
		Service.ClientState.TerritoryChanged += this.ClientStateOnTerritoryChanged;

		this.Api = new VisibilityApi();
		this.IpcProvider = new VisibilityProvider(this.Api);
	}

	private void ClientStateOnTerritoryChanged(ushort e)
	{
		this.frameworkHandler.OnTerritoryChanged();

		if (this.Configuration.AdvancedEnabled == false)
		{
			return;
		}

		this.Configuration.Enabled = false;
		this.Configuration.UpdateCurrentConfig(Service.ClientState.TerritoryType);
		this.Configuration.Enabled = true;
	}

	private void FrameworkOnOnUpdateEvent(IFramework framework)
	{
		if (this.Disable)
		{
			this.frameworkHandler.ShowAll();

			this.Disable = false;

			if (this.refresh)
			{
				Task.Run(
					async () =>
					{
						await Task.Delay(250);
						this.Configuration.Enabled = true;
						Service.ChatGui.Print(this.PluginLocalization.RefreshComplete);
					});
			}

			this.refresh = false;
		}
		else if (this.refresh)
		{
			this.Disable = true;
			this.Configuration.Enabled = false;
		}
		else
		{
			this.frameworkHandler.Update();
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		this.WindowSystem.RemoveAllWindows();
		this.IpcProvider.Dispose();
		this.Api.Dispose();

		// TODO: Switch to dalamud service
		// this.ContextMenu.Dispose();

		Service.ClientState.TerritoryChanged -= this.ClientStateOnTerritoryChanged;
		Service.Framework.Update -= this.FrameworkOnOnUpdateEvent;
		Service.PluginInterface.UiBuilder.Draw -= this.BuildUi;
		Service.PluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUi;
		Service.ChatGui.ChatMessage -= this.OnChatMessage;
		Service.CommandManager.RemoveHandler(PluginCommandName);
		Service.CommandManager.RemoveHandler(VoidCommandName);
		Service.CommandManager.RemoveHandler(VoidTargetCommandName);
		Service.CommandManager.RemoveHandler(WhitelistCommandName);
		Service.CommandManager.RemoveHandler(WhitelistTargetCommandName);

		this.frameworkHandler.Dispose();
	}

	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	public void Show(UnitType unitType, ContainerType containerType) =>
		this.frameworkHandler.Show(unitType, containerType);

	public void ShowPlayers(ContainerType type) => this.frameworkHandler.ShowPlayers(type);

	public void ShowPets(ContainerType type) => this.frameworkHandler.ShowPets(type);

	public void ShowMinions(ContainerType type) => this.frameworkHandler.ShowMinions(type);

	public void ShowChocobos(ContainerType type) => this.frameworkHandler.ShowChocobos(type);

	public void ShowPlayer(uint id) => this.frameworkHandler.ShowPlayer(id);

	public void RemoveChecked(uint id) => this.frameworkHandler.RemoveChecked(id);

	public void RemoveChecked(string name)
	{
		IGameObject? gameObject = Service.ObjectTable.SingleOrDefault(
			x => x is IPlayerCharacter character && character.Name.TextValue.Equals(
				name,
				StringComparison.InvariantCultureIgnoreCase));

		if (gameObject != null)
		{
			this.frameworkHandler.RemoveChecked(gameObject.EntityId);
		}
	}

	public void ShowPlayer(string name)
	{
		IGameObject? gameObject = Service.ObjectTable.SingleOrDefault(
			x => x is IPlayerCharacter character && character.Name.TextValue.Equals(
				name,
				StringComparison.InvariantCultureIgnoreCase));

		if (gameObject != null)
		{
			this.frameworkHandler.ShowPlayer(gameObject.EntityId);
		}
	}

	private void PluginCommand(string command, string arguments)
	{
		if (this.refresh)
		{
			return;
		}

		if (string.IsNullOrEmpty(arguments))
		{
			this.ConfigurationWindow.Toggle();
		}
		else
		{
			string[] args = arguments.Split(new[] { ' ' }, 2);

			if (args[0].Equals("help", StringComparison.InvariantCultureIgnoreCase))
			{
				Service.ChatGui.Print(this.PluginLocalization.PluginCommandHelpMenu1);
				Service.ChatGui.Print(this.PluginLocalization.PluginCommandHelpMenu2);
				Service.ChatGui.Print(this.PluginLocalization.PluginCommandHelpMenu3);
				Service.ChatGui.Print(this.PluginLocalization.PluginCommandHelpMenu4);

				foreach (string? key in this.Configuration.SettingDictionary.Keys)
				{
					Service.ChatGui.Print($"{key}");
				}

				return;
			}

			if (args[0].Equals("refresh", StringComparison.InvariantCulture))
			{
				this.RefreshActors();
				return;
			}

			if (args.Length != 2)
			{
				Service.ChatGui.Print(this.PluginLocalization.PluginCommandHelpMenuError);
				return;
			}

			if (!this.Configuration.SettingDictionary.Keys.Any(
				    x => x.Equals(args[0], StringComparison.InvariantCultureIgnoreCase)))
			{
				Service.ChatGui.Print(this.PluginLocalization.PluginCommandHelpMenuInvalidValueError(args[0]));
				return;
			}

			bool value = false;
			bool toggle = false;

			switch (args[1].ToLowerInvariant())
			{
				case "0":
				case "off":
				case "false":
					value = false;
					break;

				case "1":
				case "on":
				case "true":
					value = true;
					break;

				case "toggle":
					toggle = true;
					break;
				default:
					Service.ChatGui.Print(this.PluginLocalization.PluginCommandHelpMenuInvalidValueError(args[1]));
					return;
			}

			this.Configuration.SettingDictionary[args[0]].Invoke(value, toggle, false);
			this.Configuration.Save();
		}
	}

	public void VoidPlayer(string command, string arguments)
	{
		if (string.IsNullOrEmpty(arguments))
		{
			Service.ChatGui.Print(this.PluginLocalization.NoArgumentsError(this.PluginLocalization.VoidListName));
			return;
		}

		string[] args = arguments.Split(new[] { ' ' }, 4);

		if (args.Length < 3)
		{
			Service.ChatGui.Print(
				this.PluginLocalization.NotEnoughArgumentsError(this.PluginLocalization.VoidListName));
			return;
		}

		World? world = Service.DataManager.GetExcelSheet<World>()?.SingleOrDefault(
			x =>
				x.DataCenter.Value?.Region != 0 &&
				x.Name.ToString().Equals(args[2], StringComparison.InvariantCultureIgnoreCase));

		if (world == default(World))
		{
			Service.ChatGui.Print(
				this.PluginLocalization.InvalidWorldNameError(this.PluginLocalization.VoidListName, args[2]));
			return;
		}

		string playerName = $"{args[0].ToUppercase()} {args[1].ToUppercase()}";

		VoidItem voidItem;
		IGameObject? playerCharacter = Service.ObjectTable.SingleOrDefault(
			x => x is IPlayerCharacter character && character.HomeWorld.Id == world.RowId &&
			     character.Name.TextValue.Equals(playerName, StringComparison.InvariantCultureIgnoreCase)) as IPlayerCharacter;

		if (playerCharacter != null)
		{
			unsafe
			{
				Character* character = (Character*)playerCharacter.Address;
				voidItem = new VoidItem
				{
					Id = character->AccountId,
					Name = character->NameString,
					HomeworldId = world.RowId,
					HomeworldName = world.Name,
					Reason = args.Length == 3 ? string.Empty : args[3],
					Manual = command == "VoidUIManual"
				};
			}
		}
		else
		{
			voidItem = new VoidItem
			{
				Name = playerName,
				HomeworldId = world.RowId,
				HomeworldName = world.Name,
				Reason = args.Length == 3 ? string.Empty : args[3],
				Manual = command == "VoidUIManual"
			};
		}

		SeString playerString = new(
			new PlayerPayload(playerName, world.RowId),
			new IconPayload(BitmapFontIcon.CrossWorld),
			new TextPayload(world.Name));

		if (!this.Configuration.VoidList.Any(
			    x =>
				    x.Name == voidItem.Name && x.HomeworldId == voidItem.HomeworldId))
		{
			this.Configuration.VoidList.Add(voidItem);
			this.Configuration.Save();

			if (playerCharacter != null)
			{
				this.RemoveChecked(playerCharacter.EntityId);
			}

			Service.ChatGui.Print(
				this.PluginLocalization.EntryAdded(this.PluginLocalization.VoidListName, playerString));
		}
		else
		{
			Service.ChatGui.Print(
				this.PluginLocalization.EntryExistsError(this.PluginLocalization.VoidListName, playerString));
		}
	}

	public void VoidTargetPlayer(string command, string arguments)
	{
		if (Service.ObjectTable.SingleOrDefault(
			    x => x is IPlayerCharacter
			         && x.EntityId != 0
			         && x.EntityId != Service.ClientState.LocalPlayer?.EntityId
			         && x.EntityId == Service.ClientState.LocalPlayer?.TargetObjectId) is IPlayerCharacter
		    playerCharacter)
		{
			VoidItem voidItem;

			unsafe
			{
				Character* character = (Character*)playerCharacter.Address;
				voidItem = new VoidItem
				{
					Id = character->AccountId,
					Name = character->NameString,
					HomeworldId = character->HomeWorld,
					HomeworldName = playerCharacter.HomeWorld.GameData!.Name,
					Reason = arguments,
					Manual = false
				};
			}

			SeString playerString = new(
				new PlayerPayload(playerCharacter.Name.TextValue, playerCharacter.HomeWorld.GameData!.RowId),
				new IconPayload(BitmapFontIcon.CrossWorld),
				new TextPayload(playerCharacter.HomeWorld.GameData!.Name));

			if (!this.Configuration.VoidList.Any(
				    x =>
					    x.Name == voidItem.Name && x.HomeworldId == voidItem.HomeworldId))
			{
				this.Configuration.VoidList.Add(voidItem);
				this.Configuration.Save();
				this.RemoveChecked(playerCharacter.EntityId);
				Service.ChatGui.Print(
					this.PluginLocalization.EntryAdded(this.PluginLocalization.VoidListName, playerString));
			}
			else
			{
				Service.ChatGui.Print(
					this.PluginLocalization.EntryExistsError(this.PluginLocalization.VoidListName, playerString));
			}
		}
		else
		{
			Service.ChatGui.Print(this.PluginLocalization.InvalidTargetError(this.PluginLocalization.VoidListName));
		}
	}

	public void WhitelistPlayer(string command, string arguments)
	{
		if (string.IsNullOrEmpty(arguments))
		{
			Service.ChatGui.Print(this.PluginLocalization.NoArgumentsError(this.PluginLocalization.WhitelistName));
			return;
		}

		string[] args = arguments.Split(new[] { ' ' }, 4);

		if (args.Length < 3)
		{
			Service.ChatGui.Print(
				this.PluginLocalization.NotEnoughArgumentsError(this.PluginLocalization.WhitelistName));
			return;
		}

		World? world = Service.DataManager.GetExcelSheet<World>()?.SingleOrDefault(
			x =>
				x.DataCenter.Value?.Region != 0 &&
				x.Name.ToString().Equals(args[2], StringComparison.InvariantCultureIgnoreCase));

		if (world == default(World))
		{
			Service.ChatGui.Print(
				this.PluginLocalization.InvalidWorldNameError(this.PluginLocalization.WhitelistName, args[2]));
			return;
		}

		string playerName = $"{args[0].ToUppercase()} {args[1].ToUppercase()}";

		IPlayerCharacter? playerCharacter = Service.ObjectTable.SingleOrDefault(
			x =>
				x is IPlayerCharacter character && character.HomeWorld.Id == world.RowId &&
				character.Name.TextValue.Equals(playerName, StringComparison.Ordinal)) as IPlayerCharacter;

		VoidItem item;

		if (playerCharacter != null)
		{
			unsafe
			{
				Character* character = (Character*)playerCharacter.Address;
				item = new VoidItem
				{
					Id = character->ContentId,
					Name = character->NameString,
					HomeworldId = world.RowId,
					HomeworldName = world.Name,
					Reason = args.Length == 3 ? string.Empty : args[3],
					Manual = command == "WhitelistUIManual"
				};
			}
		}
		else
		{
			item = new VoidItem
			{
				Name = playerName,
				HomeworldId = world.RowId,
				HomeworldName = world.Name,
				Reason = args.Length == 3 ? string.Empty : args[3],
				Manual = command == "WhitelistUIManual"
			};
		}

		SeString playerString = new(
			new PlayerPayload(playerName, world.RowId),
			new IconPayload(BitmapFontIcon.CrossWorld),
			new TextPayload(world.Name));

		if (!this.Configuration.Whitelist.Any(
			    x =>
				    x.Name == item.Name && x.HomeworldId == item.HomeworldId))
		{
			this.Configuration.Whitelist.Add(item);
			this.Configuration.Save();

			if (playerCharacter != null)
			{
				this.RemoveChecked(playerCharacter.EntityId);
				this.ShowPlayer(playerCharacter.EntityId);
			}

			Service.ChatGui.Print(
				this.PluginLocalization.EntryAdded(this.PluginLocalization.WhitelistName, playerString));
		}
		else
		{
			Service.ChatGui.Print(
				this.PluginLocalization.EntryExistsError(this.PluginLocalization.WhitelistName, playerString));
		}
	}

	public void WhitelistTargetPlayer(string command, string arguments)
	{
		if (Service.ObjectTable.SingleOrDefault(
			    x => x is IPlayerCharacter
			         && x.EntityId != 0
			         && x.EntityId != Service.ClientState.LocalPlayer?.EntityId
			         && x.EntityId == Service.ClientState.LocalPlayer?.TargetObjectId) is IPlayerCharacter
		    playerCharacter)
		{
			VoidItem item;

			unsafe
			{
				Character* character = (Character*)playerCharacter.Address;
				item = new VoidItem
				{
					Id = character->ContentId,
					Name = character->NameString,
					HomeworldId = character->HomeWorld,
					HomeworldName = playerCharacter.HomeWorld.GameData!.Name,
					Reason = arguments,
					Manual = false
				};
			}

			SeString playerString = new(
				new PlayerPayload(playerCharacter.Name.TextValue, playerCharacter.HomeWorld.GameData!.RowId),
				new IconPayload(BitmapFontIcon.CrossWorld),
				new TextPayload(playerCharacter.HomeWorld.GameData!.Name));

			if (!this.Configuration.Whitelist.Any(
				    x =>
					    x.Name == item.Name && x.HomeworldId == item.HomeworldId))
			{
				this.Configuration.Whitelist.Add(item);
				this.Configuration.Save();
				this.RemoveChecked(playerCharacter.EntityId);
				this.ShowPlayer(playerCharacter.EntityId);
				Service.ChatGui.Print(
					this.PluginLocalization.EntryAdded(this.PluginLocalization.WhitelistName, playerString));
			}
			else
			{
				Service.ChatGui.Print(
					this.PluginLocalization.EntryExistsError(this.PluginLocalization.WhitelistName, playerString));
			}
		}
		else
		{
			Service.ChatGui.Print(this.PluginLocalization.InvalidTargetError(this.PluginLocalization.WhitelistName));
		}
	}

	private void OpenConfigUi() => this.ConfigurationWindow.Toggle();

	private void BuildUi() => this.WindowSystem.Draw();

	private void OnChatMessage(
		XivChatType type,
		int _,
		ref SeString sender,
		ref SeString message,
		ref bool isHandled)
	{
		if (!this.Configuration.Enabled)
		{
			return;
		}

		try
		{
			if (isHandled)
			{
				return;
			}

			PlayerPayload? playerPayload = sender.Payloads.SingleOrDefault(x => x is PlayerPayload) as PlayerPayload;
			PlayerPayload? emotePlayerPayload =
				message.Payloads.FirstOrDefault(x => x is PlayerPayload) as PlayerPayload;
			bool isEmoteType = type is XivChatType.CustomEmote or XivChatType.StandardEmote;

			if (playerPayload == default(PlayerPayload) &&
			    (!isEmoteType || emotePlayerPayload == default(PlayerPayload)))
			{
				return;
			}

			if (this.Configuration.VoidList.Any(
				    x =>
					    x.HomeworldId ==
					    (isEmoteType ? emotePlayerPayload?.World.RowId : playerPayload?.World.RowId)
					    && x.Name == (isEmoteType ? emotePlayerPayload?.PlayerName : playerPayload?.PlayerName)))
			{
				isHandled = true;
			}
		}
		catch (Exception)
		{
			// Ignore exception
		}
	}

	public void RefreshActors() => this.refresh = this.Configuration.Enabled;
}
