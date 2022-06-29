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

		public Localization PluginLocalization;
		public DalamudPluginInterface PluginInterface;
		public VisibilityConfiguration Configuration;
		public CommandManager CommandManager;
		public Framework Framework;
		public ChatGui ChatGui;
		public GameGui GameGui;
		public SigScanner SigScanner;
		public ClientState ClientState;
		public ObjectTable ObjectTable;
		public DataManager DataManager;
		public Dalamud.Game.ClientState.Conditions.Condition Condition;

		private bool drawConfig;
		private bool refresh;
		public bool Disable;

		private CharacterDrawResolver _characterDrawResolver;

		public VisibilityApi Api { get; }

		public VisibilityProvider IpcProvider { get; }

		public VisibilityPlugin(
			DalamudPluginInterface dalamudPluginInterface,
			CommandManager commandManager,
			Framework framework,
			ChatGui chatGui,
			GameGui gameGui,
			SigScanner sigScanner,
			ClientState clientState,
			ObjectTable objectTable,
			DataManager dataManager,
			Dalamud.Game.ClientState.Conditions.Condition condition)
		{
			this.Condition = condition;
			this.DataManager = dataManager;
			this.ObjectTable = objectTable;
			this.ClientState = clientState;
			this.SigScanner = sigScanner;
			this.ChatGui = chatGui;
			this.GameGui = gameGui;
			this.Framework = framework;
			this.CommandManager = commandManager;
			this.PluginInterface = dalamudPluginInterface;
			this.Configuration = this.PluginInterface.GetPluginConfig() as VisibilityConfiguration ??
			                     new VisibilityConfiguration();
			this.Configuration.Init(this);
			this.PluginLocalization = new Localization(this.Configuration.Language);

			this.CommandManager.AddHandler(
				PluginCommandName,
				new CommandInfo(this.PluginCommand)
				{
					HelpMessage = this.PluginLocalization.PluginCommandHelpMessage,
					ShowInHelp = true
				});

			this.CommandManager.AddHandler(
				VoidCommandName,
				new CommandInfo(this.VoidPlayer)
				{
					HelpMessage = this.PluginLocalization.VoidPlayerHelpMessage,
					ShowInHelp = true
				});

			this.CommandManager.AddHandler(
				VoidTargetCommandName,
				new CommandInfo(this.VoidTargetPlayer)
				{
					HelpMessage = this.PluginLocalization.VoidTargetPlayerHelpMessage,
					ShowInHelp = true
				});

			this.CommandManager.AddHandler(
				WhitelistCommandName,
				new CommandInfo(this.WhitelistPlayer)
				{
					HelpMessage = this.PluginLocalization.WhitelistPlayerHelpMessage,
					ShowInHelp = true
				});

			this.CommandManager.AddHandler(
				WhitelistTargetCommandName,
				new CommandInfo(this.WhitelistTargetPlayer)
				{
					HelpMessage = this.PluginLocalization.WhitelistTargetPlayerHelpMessage,
					ShowInHelp = true
				});

			this._characterDrawResolver = new CharacterDrawResolver();
			this._characterDrawResolver.Init(this);

			this.Framework.Update += this.FrameworkOnOnUpdateEvent;

			this.PluginInterface.UiBuilder.Draw += this.BuildUi;
			this.PluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUi;
			this.ChatGui.ChatMessage += this.OnChatMessage;

			this.Api = new VisibilityApi(this);
			this.IpcProvider = new VisibilityProvider(dalamudPluginInterface, this.Api);
		}

		private void FrameworkOnOnUpdateEvent(Framework framework)
		{
			if (this.Disable)
			{
				this._characterDrawResolver.ShowAll();

				this.Disable = false;

				if (this.refresh)
				{
					Task.Run(
						async () =>
						{
							await Task.Delay(250);
							this.Configuration.Enabled = true;
							this.ChatGui.Print(this.PluginLocalization.RefreshComplete);
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

			this._characterDrawResolver.Dispose();

			this.Framework.Update -= this.FrameworkOnOnUpdateEvent;
			this.PluginInterface.UiBuilder.Draw -= this.BuildUi;
			this.PluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUi;
			this.ChatGui.ChatMessage -= this.OnChatMessage;
			this.CommandManager.RemoveHandler(PluginCommandName);
			this.CommandManager.RemoveHandler(VoidCommandName);
			this.CommandManager.RemoveHandler(VoidTargetCommandName);
			this.CommandManager.RemoveHandler(WhitelistCommandName);
			this.CommandManager.RemoveHandler(WhitelistTargetCommandName);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		public void Show(UnitType unitType, ContainerType containerType)
		{
			this._characterDrawResolver.Show(unitType, containerType);
		}

		public void ShowPlayers(ContainerType type)
		{
			this._characterDrawResolver.ShowPlayers(type);
		}

		public void ShowPets(ContainerType type)
		{
			this._characterDrawResolver.ShowPets(type);
		}

		public void ShowMinions(ContainerType type)
		{
			this._characterDrawResolver.ShowMinions(type);
		}

		public void ShowChocobos(ContainerType type)
		{
			this._characterDrawResolver.ShowChocobos(type);
		}

		public void ShowPlayer(uint id)
		{
			this._characterDrawResolver.ShowPlayer(id);
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
					this.ChatGui.Print(this.PluginLocalization.PluginCommandHelpMenu1);
					this.ChatGui.Print(this.PluginLocalization.PluginCommandHelpMenu2);
					this.ChatGui.Print(this.PluginLocalization.PluginCommandHelpMenu3);
					this.ChatGui.Print(this.PluginLocalization.PluginCommandHelpMenu4);

					foreach (var key in this.Configuration.SettingDictionary.Keys)
					{
						this.ChatGui.Print($"{key}");
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
					this.ChatGui.Print(this.PluginLocalization.PluginCommandHelpMenuError);
					return;
				}

				if (!this.Configuration.SettingDictionary.Keys.Any(
					    x => x.Equals(args[0], StringComparison.InvariantCultureIgnoreCase)))
				{
					this.ChatGui.Print(this.PluginLocalization.PluginCommandHelpMenuInvalidValueError(args[0]));
					return;
				}

				int value;

				switch (args[1].ToLowerInvariant())
				{
					case "0":
					case "off":
					case "false":
						value = 0;
						break;

					case "1":
					case "on":
					case "true":
						value = 1;
						break;

					case "toggle":
						value = 2;
						break;
					default:
						this.ChatGui.Print(this.PluginLocalization.PluginCommandHelpMenuInvalidValueError(args[1]));
						return;
				}

				this.Configuration.SettingDictionary[args[0].ToLowerInvariant()].Invoke(value);
				this.Configuration.Save();
			}
		}

		public void VoidPlayer(string command, string arguments)
		{
			if (string.IsNullOrEmpty(arguments))
			{
				this.ChatGui.Print(this.PluginLocalization.NoArgumentsError(this.PluginLocalization.VoidListName));
				return;
			}

			var args = arguments.Split(new[] { ' ' }, 4);

			if (args.Length < 3)
			{
				this.ChatGui.Print(
					this.PluginLocalization.NotEnoughArgumentsError(this.PluginLocalization.VoidListName));
				return;
			}

			var world = this.DataManager.GetExcelSheet<World>()?.SingleOrDefault(
				x =>
					x.DataCenter.Value?.Region != 0 &&
					x.Name.ToString().Equals(args[2], StringComparison.InvariantCultureIgnoreCase));

			if (world == default(World))
			{
				this.ChatGui.Print(
					this.PluginLocalization.InvalidWorldNameError(this.PluginLocalization.VoidListName, args[2]));
				return;
			}

			var playerName = $"{args[0].ToUppercase()} {args[1].ToUppercase()}";

			var voidItem = !(this.ObjectTable
				.SingleOrDefault(
					x => x is PlayerCharacter character
					     && character.HomeWorld.Id == world.RowId
					     && character.Name.TextValue.Equals(
						     playerName,
						     StringComparison.InvariantCultureIgnoreCase)) is PlayerCharacter actor)
				? new VoidItem(
					playerName,
					world.Name,
					world.RowId,
					args.Length == 3 ? string.Empty : args[3],
					command == "VoidUIManual")
				: new VoidItem(actor, args.Length == 3 ? string.Empty : args[3], command == "VoidUIManual");

			var playerString = Encoding.UTF8.GetString(
				new SeString(
					new TextPayload(playerName),
					new IconPayload(BitmapFontIcon.CrossWorld),
					new TextPayload(world.Name)).Encode());

			if (!this.Configuration.VoidList.Any(
				    x =>
					    x.Name == voidItem.Name && x.HomeworldId == voidItem.HomeworldId))
			{
				this.Configuration.VoidList.Add(voidItem);
				this.Configuration.Save();
				this.ChatGui.Print(
					this.PluginLocalization.EntryAdded(this.PluginLocalization.VoidListName, playerString));
			}
			else
			{
				this.ChatGui.Print(
					this.PluginLocalization.EntryExistsError(this.PluginLocalization.VoidListName, playerString));
			}
		}

		public void VoidTargetPlayer(string command, string arguments)
		{
			if (this.ObjectTable.SingleOrDefault(
				    x => x is PlayerCharacter
				         && x.ObjectId != 0
				         && x.ObjectId != this.ClientState.LocalPlayer?.ObjectId
				         && x.ObjectId == this.ClientState.LocalPlayer?.TargetObjectId) is PlayerCharacter actor)
			{
				var voidItem = new VoidItem(actor, arguments, false);

				var playerString = Encoding.UTF8.GetString(
					new SeString(
						new TextPayload(actor.Name.TextValue),
						new IconPayload(BitmapFontIcon.CrossWorld),
						new TextPayload(actor.HomeWorld.GameData!.Name)).Encode());

				if (!this.Configuration.VoidList.Any(
					    x =>
						    x.Name == voidItem.Name && x.HomeworldId == voidItem.HomeworldId))
				{
					this.Configuration.VoidList.Add(voidItem);
					this.Configuration.Save();
					this.ChatGui.Print(
						this.PluginLocalization.EntryAdded(this.PluginLocalization.VoidListName, playerString));
				}
				else
				{
					this.ChatGui.Print(
						this.PluginLocalization.EntryExistsError(this.PluginLocalization.VoidListName, playerString));
				}
			}
			else
			{
				this.ChatGui.Print(this.PluginLocalization.InvalidTargetError(this.PluginLocalization.VoidListName));
			}
		}

		public void WhitelistPlayer(string command, string arguments)
		{
			if (string.IsNullOrEmpty(arguments))
			{
				this.ChatGui.Print(this.PluginLocalization.NoArgumentsError(this.PluginLocalization.WhitelistName));
				return;
			}

			var args = arguments.Split(new[] { ' ' }, 4);

			if (args.Length < 3)
			{
				this.ChatGui.Print(
					this.PluginLocalization.NotEnoughArgumentsError(this.PluginLocalization.WhitelistName));
				return;
			}

			var world = this.DataManager.GetExcelSheet<World>()?.SingleOrDefault(
				x =>
					x.DataCenter.Value?.Region != 0 &&
					x.Name.ToString().Equals(args[2], StringComparison.InvariantCultureIgnoreCase));

			if (world == default(World))
			{
				this.ChatGui.Print(
					this.PluginLocalization.InvalidWorldNameError(this.PluginLocalization.WhitelistName, args[2]));
				return;
			}

			var playerName = $"{args[0].ToUppercase()} {args[1].ToUppercase()}";

			var actor = this.ObjectTable.SingleOrDefault(
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

			var playerString = Encoding.UTF8.GetString(
				new SeString(
					new TextPayload(playerName),
					new IconPayload(BitmapFontIcon.CrossWorld),
					new TextPayload(world.Name)).Encode());

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

				this.ChatGui.Print(
					this.PluginLocalization.EntryAdded(this.PluginLocalization.WhitelistName, playerString));
			}
			else
			{
				this.ChatGui.Print(
					this.PluginLocalization.EntryExistsError(this.PluginLocalization.WhitelistName, playerString));
			}
		}

		public void WhitelistTargetPlayer(string command, string arguments)
		{
			if (this.ObjectTable.SingleOrDefault(
				    x => x is PlayerCharacter
				         && x.ObjectId != 0
				         && x.ObjectId != this.ClientState.LocalPlayer?.ObjectId
				         && x.ObjectId == this.ClientState.LocalPlayer?.TargetObjectId) is PlayerCharacter actor)
			{
				var item = new VoidItem(actor, arguments, false);

				var playerString = Encoding.UTF8.GetString(
					new SeString(
						new TextPayload(actor.Name.TextValue),
						new IconPayload(BitmapFontIcon.CrossWorld),
						new TextPayload(actor.HomeWorld.GameData!.Name)).Encode());

				if (!this.Configuration.Whitelist.Any(
					    x =>
						    x.Name == item.Name && x.HomeworldId == item.HomeworldId))
				{
					this.Configuration.Whitelist.Add(item);
					this.Configuration.Save();
					this.ShowPlayer(actor.ObjectId);
					this.ChatGui.Print(
						this.PluginLocalization.EntryAdded(this.PluginLocalization.WhitelistName, playerString));
				}
				else
				{
					this.ChatGui.Print(
						this.PluginLocalization.EntryExistsError(this.PluginLocalization.WhitelistName, playerString));
				}
			}
			else
			{
				this.ChatGui.Print(this.PluginLocalization.InvalidTargetError(this.PluginLocalization.WhitelistName));
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