using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using Visibility.Api;
using Visibility.Configuration;
using Visibility.Ipc;
using Visibility.Utils;
using Visibility.Void;

namespace Visibility
{
	public class VisibilityPlugin : IDalamudPlugin
	{
		public string Name => "Visibility";

		private static string PluginCommandName => "/pvis";

		private static string VoidCommandName => "/void";

		private static string VoidTargetCommandName => "/voidtarget";

		private static string WhitelistCommandName => "/whitelist";

		private static string WhitelistTargetCommandName => "/whitelisttarget";

		#region Plugin Services

		[PluginService]
		[RequiredVersion("1.0")]
		public static DalamudPluginInterface PluginInterface { get; set; } = null!;

		[PluginService]
		[RequiredVersion("1.0")]
		public static CommandManager CommandManager { get; set; } = null!;

		[PluginService]
		[RequiredVersion("1.0")]
		public static ChatGui ChatGui { get; set; } = null!;

		[PluginService]
		[RequiredVersion("1.0")]
		public static DataManager DataManager { get; set; } = null!;

		[PluginService]
		[RequiredVersion("1.0")]
		public static SigScanner SigScanner { get; set; } = null!;

		[PluginService]
		[RequiredVersion("1.0")]
		public static GameGui GameGui { get; set; } = null!;

		[PluginService]
		[RequiredVersion("1.0")]
		public static ClientState ClientState { get; set; } = null!;

		[PluginService]
		[RequiredVersion("1.0")]
		public static Framework Framework { get; set; } = null!;

		[PluginService]
		[RequiredVersion("1.0")]
		public static ObjectTable ObjectTable { get; set; } = null!;

		[PluginService]
		[RequiredVersion("1.0")]
		public static Dalamud.Game.ClientState.Conditions.Condition Condition { get; set; } = null!;

		#endregion

		public readonly Localization PluginLocalization;
		public readonly VisibilityConfiguration Configuration;
		
		public readonly ContextMenu ContextMenu;
		public static VisibilityPlugin Instance { get; private set; } = null!;

		private bool drawConfig;
		private bool refresh;
		public bool Disable;

		private readonly CharacterDrawResolver characterDrawResolver;

		public VisibilityApi Api { get; }

		public VisibilityProvider IpcProvider { get; }

		public VisibilityPlugin()
		{
			Instance = this;
			this.Configuration = PluginInterface.GetPluginConfig() as VisibilityConfiguration ??
			                     new VisibilityConfiguration();
			this.Configuration.Init(ClientState.TerritoryType);
			this.PluginLocalization = new Localization(this.Configuration.Language);
			this.ContextMenu = new ContextMenu();

			if (this.Configuration.EnableContextMenu)
			{
				this.ContextMenu.Toggle();
			}

			CommandManager.AddHandler(
				PluginCommandName,
				new CommandInfo(this.PluginCommand)
				{
					HelpMessage = this.PluginLocalization.PluginCommandHelpMessage,
					ShowInHelp = true
				});

			CommandManager.AddHandler(
				VoidCommandName,
				new CommandInfo(this.VoidPlayer)
				{
					HelpMessage = this.PluginLocalization.VoidPlayerHelpMessage,
					ShowInHelp = true
				});

			CommandManager.AddHandler(
				VoidTargetCommandName,
				new CommandInfo(this.VoidTargetPlayer)
				{
					HelpMessage = this.PluginLocalization.VoidTargetPlayerHelpMessage,
					ShowInHelp = true
				});

			CommandManager.AddHandler(
				WhitelistCommandName,
				new CommandInfo(this.WhitelistPlayer)
				{
					HelpMessage = this.PluginLocalization.WhitelistPlayerHelpMessage,
					ShowInHelp = true
				});

			CommandManager.AddHandler(
				WhitelistTargetCommandName,
				new CommandInfo(this.WhitelistTargetPlayer)
				{
					HelpMessage = this.PluginLocalization.WhitelistTargetPlayerHelpMessage,
					ShowInHelp = true
				});

			this.characterDrawResolver = new CharacterDrawResolver();
			this.characterDrawResolver.Init();

			Framework.Update += this.FrameworkOnOnUpdateEvent;

			PluginInterface.UiBuilder.Draw += this.BuildUi;
			PluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUi;
			ChatGui.ChatMessage += this.OnChatMessage;
			ClientState.TerritoryChanged += this.ClientStateOnTerritoryChanged;

			this.Api = new VisibilityApi();
			this.IpcProvider = new VisibilityProvider(this.Api);
		}

		private void ClientStateOnTerritoryChanged(object? sender, ushort e)
		{
			if (this.Configuration.AdvancedEnabled == false)
			{
				return;
			}

			this.Configuration.Enabled = false;
			this.Configuration.UpdateCurrentConfig(ClientState.TerritoryType);
			this.Configuration.Enabled = true;
		}

		private void FrameworkOnOnUpdateEvent(Framework framework)
		{
			if (this.Disable)
			{
				this.characterDrawResolver.ShowAll();

				this.Disable = false;

				if (this.refresh)
				{
					Task.Run(
						async () =>
						{
							await Task.Delay(250);
							this.Configuration.Enabled = true;
							ChatGui.Print(this.PluginLocalization.RefreshComplete);
						});
				}

				this.refresh = false;
			}
			else if (this.refresh)
			{
				this.Disable = true;
				this.Configuration.Enabled = false;
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			this.IpcProvider.Dispose();
			this.Api.Dispose();
			this.ContextMenu.Dispose();

			this.characterDrawResolver.Dispose();

			ClientState.TerritoryChanged -= this.ClientStateOnTerritoryChanged;
			Framework.Update -= this.FrameworkOnOnUpdateEvent;
			PluginInterface.UiBuilder.Draw -= this.BuildUi;
			PluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUi;
			ChatGui.ChatMessage -= this.OnChatMessage;
			CommandManager.RemoveHandler(PluginCommandName);
			CommandManager.RemoveHandler(VoidCommandName);
			CommandManager.RemoveHandler(VoidTargetCommandName);
			CommandManager.RemoveHandler(WhitelistCommandName);
			CommandManager.RemoveHandler(WhitelistTargetCommandName);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		public void Show(UnitType unitType, ContainerType containerType)
		{
			this.characterDrawResolver.Show(unitType, containerType);
		}

		public void ShowPlayers(ContainerType type)
		{
			this.characterDrawResolver.ShowPlayers(type);
		}

		public void ShowPets(ContainerType type)
		{
			this.characterDrawResolver.ShowPets(type);
		}

		public void ShowMinions(ContainerType type)
		{
			this.characterDrawResolver.ShowMinions(type);
		}

		public void ShowChocobos(ContainerType type)
		{
			this.characterDrawResolver.ShowChocobos(type);
		}

		public void ShowPlayer(uint id)
		{
			this.characterDrawResolver.ShowPlayer(id);
		}

		private void PluginCommand(string command, string arguments)
		{
			if (this.refresh)
			{
				return;
			}

			if (string.IsNullOrEmpty(arguments))
			{
				this.drawConfig = !this.drawConfig;
			}
			else
			{
				var args = arguments.Split(new[] { ' ' }, 2);

				if (args[0].Equals("help", StringComparison.InvariantCultureIgnoreCase))
				{
					ChatGui.Print(this.PluginLocalization.PluginCommandHelpMenu1);
					ChatGui.Print(this.PluginLocalization.PluginCommandHelpMenu2);
					ChatGui.Print(this.PluginLocalization.PluginCommandHelpMenu3);
					ChatGui.Print(this.PluginLocalization.PluginCommandHelpMenu4);

					foreach (var key in this.Configuration.SettingDictionary.Keys)
					{
						ChatGui.Print($"{key}");
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
					ChatGui.Print(this.PluginLocalization.PluginCommandHelpMenuError);
					return;
				}

				if (!this.Configuration.SettingDictionary.Keys.Any(
					    x => x.Equals(args[0], StringComparison.InvariantCultureIgnoreCase)))
				{
					ChatGui.Print(
						this.PluginLocalization.PluginCommandHelpMenuInvalidValueError(args[0]));
					return;
				}

				var value = false;
				var toggle = false;

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
						ChatGui.Print(
							this.PluginLocalization.PluginCommandHelpMenuInvalidValueError(args[1]));
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
				ChatGui.Print(
					this.PluginLocalization.NoArgumentsError(this.PluginLocalization.VoidListName));
				return;
			}

			var args = arguments.Split(new[] { ' ' }, 4);

			if (args.Length < 3)
			{
				ChatGui.Print(
					this.PluginLocalization.NotEnoughArgumentsError(this.PluginLocalization.VoidListName));
				return;
			}

			var world = DataManager.GetExcelSheet<World>()?.SingleOrDefault(
				x =>
					x.DataCenter.Value?.Region != 0 &&
					x.Name.ToString().Equals(args[2], StringComparison.InvariantCultureIgnoreCase));

			if (world == default(World))
			{
				ChatGui.Print(
					this.PluginLocalization.InvalidWorldNameError(this.PluginLocalization.VoidListName, args[2]));
				return;
			}

			var playerName = $"{args[0].ToUppercase()} {args[1].ToUppercase()}";

			var voidItem = ObjectTable
				.SingleOrDefault(
					x => x is PlayerCharacter character
					     && character.HomeWorld.Id == world.RowId
					     && character.Name.TextValue.Equals(
						     playerName,
						     StringComparison.InvariantCultureIgnoreCase)) is not PlayerCharacter actor
				? new VoidItem(
					playerName,
					world.Name,
					world.RowId,
					args.Length == 3 ? string.Empty : args[3],
					command == "VoidUIManual")
				: new VoidItem(actor, args.Length == 3 ? string.Empty : args[3], command == "VoidUIManual");

			var playerString = new SeString(
				new PlayerPayload(playerName, world.RowId),
				new IconPayload(BitmapFontIcon.CrossWorld),
				new TextPayload(world.Name));

			if (!this.Configuration.VoidList.Any(
				    x =>
					    x.Name == voidItem.Name && x.HomeworldId == voidItem.HomeworldId))
			{
				this.Configuration.VoidList.Add(voidItem);
				this.Configuration.Save();
				ChatGui.Print(
					this.PluginLocalization.EntryAdded(this.PluginLocalization.VoidListName, playerString));
			}
			else
			{
				ChatGui.Print(
					this.PluginLocalization.EntryExistsError(this.PluginLocalization.VoidListName, playerString));
			}
		}

		public void VoidTargetPlayer(string command, string arguments)
		{
			if (ObjectTable.SingleOrDefault(
				    x => x is PlayerCharacter
				         && x.ObjectId != 0
				         && x.ObjectId != ClientState.LocalPlayer?.ObjectId
				         && x.ObjectId == ClientState.LocalPlayer?.TargetObjectId) is PlayerCharacter
			    actor)
			{
				var voidItem = new VoidItem(actor, arguments, false);

				var playerString = new SeString(
					new PlayerPayload(actor.Name.TextValue, actor.HomeWorld.GameData!.RowId),
					new IconPayload(BitmapFontIcon.CrossWorld),
					new TextPayload(actor.HomeWorld.GameData!.Name));

				if (!this.Configuration.VoidList.Any(
					    x =>
						    x.Name == voidItem.Name && x.HomeworldId == voidItem.HomeworldId))
				{
					this.Configuration.VoidList.Add(voidItem);
					this.Configuration.Save();
					ChatGui.Print(
						this.PluginLocalization.EntryAdded(this.PluginLocalization.VoidListName, playerString));
				}
				else
				{
					ChatGui.Print(
						this.PluginLocalization.EntryExistsError(this.PluginLocalization.VoidListName, playerString));
				}
			}
			else
			{
				ChatGui.Print(
					this.PluginLocalization.InvalidTargetError(this.PluginLocalization.VoidListName));
			}
		}

		public void WhitelistPlayer(string command, string arguments)
		{
			if (string.IsNullOrEmpty(arguments))
			{
				ChatGui.Print(
					this.PluginLocalization.NoArgumentsError(this.PluginLocalization.WhitelistName));
				return;
			}

			var args = arguments.Split(new[] { ' ' }, 4);

			if (args.Length < 3)
			{
				ChatGui.Print(
					this.PluginLocalization.NotEnoughArgumentsError(this.PluginLocalization.WhitelistName));
				return;
			}

			var world = DataManager.GetExcelSheet<World>()?.SingleOrDefault(
				x =>
					x.DataCenter.Value?.Region != 0 &&
					x.Name.ToString().Equals(args[2], StringComparison.InvariantCultureIgnoreCase));

			if (world == default(World))
			{
				ChatGui.Print(
					this.PluginLocalization.InvalidWorldNameError(this.PluginLocalization.WhitelistName, args[2]));
				return;
			}

			var playerName = $"{args[0].ToUppercase()} {args[1].ToUppercase()}";

			var actor = ObjectTable.SingleOrDefault(
				x =>
					x is PlayerCharacter character && character.HomeWorld.Id == world.RowId &&
					character.Name.TextValue.Equals(playerName, StringComparison.Ordinal)) as PlayerCharacter;

			var item = actor == null
				? new VoidItem(
					playerName,
					world.Name,
					world.RowId,
					args.Length == 3 ? string.Empty : args[3],
					command == "WhitelistUIManual")
				: new VoidItem(actor, args.Length == 3 ? string.Empty : args[3], command == "WhitelistUIManual");

			var playerString = new SeString(
				new PlayerPayload(playerName, world.RowId),
				new IconPayload(BitmapFontIcon.CrossWorld),
				new TextPayload(world.Name));

			if (!this.Configuration.Whitelist.Any(
				    x =>
					    x.Name == item.Name && x.HomeworldId == item.HomeworldId))
			{
				this.Configuration.Whitelist.Add(item);
				this.Configuration.Save();

				if (actor != null)
				{
					this.ShowPlayer(actor.ObjectId);
				}

				ChatGui.Print(
					this.PluginLocalization.EntryAdded(this.PluginLocalization.WhitelistName, playerString));
			}
			else
			{
				ChatGui.Print(
					this.PluginLocalization.EntryExistsError(this.PluginLocalization.WhitelistName, playerString));
			}
		}

		public void WhitelistTargetPlayer(string command, string arguments)
		{
			if (ObjectTable.SingleOrDefault(
				    x => x is PlayerCharacter
				         && x.ObjectId != 0
				         && x.ObjectId != ClientState.LocalPlayer?.ObjectId
				         && x.ObjectId == ClientState.LocalPlayer?.TargetObjectId) is PlayerCharacter
			    actor)
			{
				var item = new VoidItem(actor, arguments, false);

				var playerString = new SeString(
					new PlayerPayload(actor.Name.TextValue, actor.HomeWorld.GameData!.RowId),
					new IconPayload(BitmapFontIcon.CrossWorld),
					new TextPayload(actor.HomeWorld.GameData!.Name));

				if (!this.Configuration.Whitelist.Any(
					    x =>
						    x.Name == item.Name && x.HomeworldId == item.HomeworldId))
				{
					this.Configuration.Whitelist.Add(item);
					this.Configuration.Save();
					this.ShowPlayer(actor.ObjectId);
					ChatGui.Print(
						this.PluginLocalization.EntryAdded(this.PluginLocalization.WhitelistName, playerString));
				}
				else
				{
					ChatGui.Print(
						this.PluginLocalization.EntryExistsError(this.PluginLocalization.WhitelistName, playerString));
				}
			}
			else
			{
				ChatGui.Print(
					this.PluginLocalization.InvalidTargetError(this.PluginLocalization.WhitelistName));
			}
		}

		private void OpenConfigUi()
		{
			this.drawConfig = !this.drawConfig;
		}

		private void BuildUi()
		{
			this.drawConfig = this.drawConfig && this.Configuration.DrawConfigUi();
		}

		private void OnChatMessage(
			XivChatType type,
			uint senderId,
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

				var playerPayload = sender.Payloads.SingleOrDefault(x => x is PlayerPayload) as PlayerPayload;
				var emotePlayerPayload = message.Payloads.FirstOrDefault(x => x is PlayerPayload) as PlayerPayload;
				var isEmoteType = type is XivChatType.CustomEmote or XivChatType.StandardEmote;

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

		public void RefreshActors()
		{
			this.refresh = this.Configuration.Enabled;
		}
	}
}