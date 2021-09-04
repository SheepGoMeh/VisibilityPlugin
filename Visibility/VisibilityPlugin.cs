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
using Visibility.Configuration;
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

		public DalamudPluginInterface PluginInterface;
		public VisibilityConfiguration Configuration;
		public CommandManager CommandManager;
		public Framework Framework;
		public ChatGui ChatGui;
		public SigScanner SigScanner;
		public ClientState ClientState;
		public ObjectTable ObjectTable;
		public DataManager DataManager;
		public Dalamud.Game.ClientState.Conditions.Condition Condition;

		private bool _drawConfig;
		private bool _refresh;
		public bool Disable;
		
		private CharacterDrawResolver _characterDrawResolver;

		public VisibilityPlugin(DalamudPluginInterface dalamudPluginInterface, CommandManager commandManager,
			Framework framework, ChatGui chatGui, SigScanner sigScanner, ClientState clientState,
			ObjectTable objectTable, DataManager dataManager, Dalamud.Game.ClientState.Conditions.Condition condition)
		{
			Condition = condition;
			DataManager = dataManager;
			ObjectTable = objectTable;
			ClientState = clientState;
			SigScanner = sigScanner;
			ChatGui = chatGui;
			Framework = framework;
			CommandManager = commandManager;
			PluginInterface = dalamudPluginInterface;
			Configuration = PluginInterface.GetPluginConfig() as VisibilityConfiguration ?? new VisibilityConfiguration();
			Configuration.Init(this);

			CommandManager.AddHandler(PluginCommandName, new CommandInfo(PluginCommand)
			{
				HelpMessage = $"Shows the config for the visibility plugin.\nAdditional help available via '{PluginCommandName} help'",
				ShowInHelp = true
			});

			CommandManager.AddHandler(VoidCommandName, new CommandInfo(VoidPlayer)
			{
				HelpMessage = $"Adds player to void list.\nUsage: {VoidCommandName} <firstname> <lastname> <worldname> <reason>",
				ShowInHelp = true
			});
			
			CommandManager.AddHandler(VoidTargetCommandName, new CommandInfo(VoidTargetPlayer)
			{
				HelpMessage = $"Adds targeted player to void list.\nUsage: {VoidTargetCommandName} <reason>",
				ShowInHelp = true
			});
			
			CommandManager.AddHandler(WhitelistCommandName, new CommandInfo(WhitelistPlayer)
			{
				HelpMessage = $"Adds player to whitelist.\nUsage: {WhitelistCommandName} <firstname> <lastname> <worldname>",
				ShowInHelp = true
			});
			
			CommandManager.AddHandler(WhitelistTargetCommandName, new CommandInfo(WhitelistTargetPlayer)
			{
				HelpMessage = $"Adds targeted player to whitelist.\nUsage: {WhitelistTargetCommandName}",
				ShowInHelp = true
			});

			_characterDrawResolver = new CharacterDrawResolver();
			_characterDrawResolver.Init(this);

			Framework.Update += FrameworkOnOnUpdateEvent;
			
			PluginInterface.UiBuilder.Draw += BuildUi;
			PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
			ChatGui.ChatMessage += OnChatMessage;
		}

		private void FrameworkOnOnUpdateEvent(Framework framework)
		{
			if (Disable)
			{
				_characterDrawResolver.ShowAll();

				Disable = false;

				if (_refresh)
				{
					Task.Run(async () =>
					{
						await Task.Delay(250);
						Configuration.Enabled = true;
						ChatGui.Print("Refresh complete.");
					});
				}
				
				_refresh = false;
			}
			else if (_refresh)
			{
				Disable = true;
				Configuration.Enabled = false;
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			_characterDrawResolver.Dispose();

			Framework.Update -= FrameworkOnOnUpdateEvent;
			PluginInterface.UiBuilder.Draw -= BuildUi;
			PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
			ChatGui.ChatMessage -= OnChatMessage;
			CommandManager.RemoveHandler(PluginCommandName);
			CommandManager.RemoveHandler(VoidCommandName);
			CommandManager.RemoveHandler(VoidTargetCommandName);
			CommandManager.RemoveHandler(WhitelistCommandName);
			CommandManager.RemoveHandler(WhitelistTargetCommandName);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public void Show(UnitType unitType, ContainerType containerType)
		{
			_characterDrawResolver.Show(unitType, containerType);
		}

		public void ShowPlayers(ContainerType type)
		{
			_characterDrawResolver.ShowPlayers(type);
		}
		
		public void ShowPets(ContainerType type)
		{
			_characterDrawResolver.ShowPets(type);
		}

		public void ShowMinions(ContainerType type)
		{
			_characterDrawResolver.ShowMinions(type);
		}

		public void ShowChocobos(ContainerType type)
		{
			_characterDrawResolver.ShowChocobos(type);
		}

		public void ShowPlayer(uint id)
		{
			_characterDrawResolver.ShowPlayer(id);
		}

		private void PluginCommand(string command, string arguments)
		{
			if (_refresh)
			{
				return;
			}

			if (string.IsNullOrEmpty(arguments))
			{
				_drawConfig = !_drawConfig;
			}
			else
			{
				var args = arguments.Split(new[] { ' ' }, 2);

				if (args[0].Equals("help", StringComparison.InvariantCultureIgnoreCase))
				{
					ChatGui.Print($"{PluginCommandName} help - This help menu");
					ChatGui.Print($"{PluginCommandName} refresh - Refreshes hidden actors");
					ChatGui.Print($"{PluginCommandName} <setting> <on/off/toggle> - Sets a setting to on, off or toggles it");
					ChatGui.Print("Available values:");

					foreach (var key in Configuration.settingDictionary.Keys)
					{
						ChatGui.Print($"{key}");
					}
					
					return;
				}
				else if (args[0].Equals("refresh", StringComparison.InvariantCulture))
				{
					RefreshActors();
					return;
				}

				if (args.Length != 2)
				{
					ChatGui.Print("Too few arguments specified.");
					return;
				}
				
				if (!Configuration.settingDictionary.Keys.Any(x => x.Equals(args[0], StringComparison.InvariantCultureIgnoreCase)))
				{
					ChatGui.Print($"'{args[0]}' is not a valid value.");
					return;
				}

				int value;

				switch(args[1].ToLowerInvariant())
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
						ChatGui.Print($"'{args[1]}' is not a valid value.");
						return;
				}

				Configuration.settingDictionary[args[0].ToLowerInvariant()].Invoke(value);
				Configuration.Save();
			}
		}

		public void VoidPlayer(string command, string arguments)
		{
			if (string.IsNullOrEmpty(arguments))
			{
				ChatGui.Print("VoidList: No arguments specified.");
				return;
			}

			var args = arguments.Split(new[] { ' ' }, 4);

			if (args.Length < 3)
			{
				ChatGui.Print("VoidList: Too few arguments specified.");
				return;
			}

			var world = DataManager.GetExcelSheet<World>().SingleOrDefault(x =>
				x.DataCenter.Value.Region != 0 &&
				x.Name.ToString().Equals(args[2], StringComparison.InvariantCultureIgnoreCase));

			if (world == default(World))
			{
				ChatGui.Print($"VoidList: '{args[2]}' is not a valid world name.");
				return;
			}

			var playerName = $"{args[0].ToUppercase()} {args[1].ToUppercase()}";

			var voidItem = (!(ObjectTable
				.SingleOrDefault(x => x is PlayerCharacter character
				                      && character.HomeWorld.Id == world.RowId
				                      && character.Name.TextValue.Equals(playerName, StringComparison.InvariantCultureIgnoreCase)) is PlayerCharacter actor)
				? new VoidItem(playerName, world.Name, world.RowId, args.Length == 3 ? string.Empty : args[3], command == "VoidUIManual")
				: new VoidItem(actor, args.Length == 3 ? string.Empty : args[3], command == "VoidUIManual"));

			var icon = Encoding.UTF8.GetString(new IconPayload(BitmapFontIcon.CrossWorld).Encode());

			if (!Configuration.VoidList.Any(x =>
				x.Name == voidItem.Name && x.HomeworldId == voidItem.HomeworldId))
			{
				Configuration.VoidList.Add(voidItem);
				Configuration.Save();
				ChatGui.Print($"VoidList: {playerName}{icon}{world.Name} has been added.");
			}
			else
			{
				ChatGui.Print($"VoidList: {playerName}{icon}{world.Name} entry already exists.");
			}
		}

		public void VoidTargetPlayer(string command, string arguments)
		{
			if (ObjectTable.SingleOrDefault(x => x is PlayerCharacter
			                                     && x.ObjectId != 0
			                                     && x.ObjectId != ClientState.LocalPlayer?.ObjectId
			                                     && x.ObjectId == ClientState.LocalPlayer?.TargetObjectId) is PlayerCharacter actor)
			{
				var voidItem = new VoidItem(actor, arguments, false);
				var icon = Encoding.UTF8.GetString(new byte[] {2, 18, 2, 89, 3});
				
				if (!Configuration.VoidList.Any(x =>
					x.Name == voidItem.Name && x.HomeworldId == voidItem.HomeworldId))
				{
					Configuration.VoidList.Add(voidItem);
					Configuration.Save();
					ChatGui.Print($"VoidList: {actor.Name}{icon}{actor.HomeWorld.GameData.Name} has been added.");
				}
				else
				{
					ChatGui.Print($"VoidList: {actor.Name}{icon}{actor.HomeWorld.GameData.Name} entry already exists.");
				}
			}
			else
			{
				ChatGui.Print("VoidList: Invalid target.");
			}
		}
		
		public void WhitelistPlayer(string command, string arguments)
		{
			if (string.IsNullOrEmpty(arguments))
			{
				ChatGui.Print("Whitelist: No arguments specified.");
				return;
			}

			var args = arguments.Split(new[] { ' ' }, 4);

			if (args.Length < 3)
			{
				ChatGui.Print("Whitelist: Too few arguments specified.");
				return;
			}

			var world = DataManager.GetExcelSheet<World>().SingleOrDefault(x =>
				x.DataCenter.Value.Region != 0 &&
				x.Name.ToString().Equals(args[2], StringComparison.InvariantCultureIgnoreCase));

			if (world == default(World))
			{
				ChatGui.Print($"Whitelist: '{args[2]}' is not a valid world name.");
				return;
			}

			var playerName = $"{args[0].ToUppercase()} {args[1].ToUppercase()}";

			var actor = ObjectTable.SingleOrDefault(x =>
					x is PlayerCharacter character && character.HomeWorld.Id == world.RowId &&
					character.Name.TextValue.Equals(playerName, StringComparison.Ordinal)) as PlayerCharacter;

			var item = actor == null
				? new VoidItem(playerName, world.Name, world.RowId, args.Length == 3 ? string.Empty : args[3], command == "WhitelistUIManual")
				: new VoidItem(actor, args.Length == 3 ? string.Empty : args[3], command == "WhitelistUIManual");

			var icon = Encoding.UTF8.GetString(new IconPayload(BitmapFontIcon.CrossWorld).Encode()); 

			if (!Configuration.Whitelist.Any(x =>
				x.Name == item.Name && x.HomeworldId == item.HomeworldId))
			{
				Configuration.Whitelist.Add(item);
				Configuration.Save();

				if (actor != null)
				{
					ShowPlayer(actor.ObjectId);
				}

				ChatGui.Print($"Whitelist: {playerName}{icon}{world.Name} has been added.");
			}
			else
			{
				ChatGui.Print($"Whitelist: {playerName}{icon}{world.Name} entry already exists.");
			}
		}

		public void WhitelistTargetPlayer(string command, string arguments)
		{
			if (ObjectTable.SingleOrDefault(x => x is PlayerCharacter
			                                     && x.ObjectId != 0
			                                     && x.ObjectId != ClientState.LocalPlayer?.ObjectId
			                                     && x.ObjectId == ClientState.LocalPlayer?.TargetObjectId) is PlayerCharacter actor)
			{
				var item = new VoidItem(actor, arguments, false);
				var icon = Encoding.UTF8.GetString(new byte[] {2, 18, 2, 89, 3});
				
				if (!Configuration.Whitelist.Any(x =>
					x.Name == item.Name && x.HomeworldId == item.HomeworldId))
				{
					Configuration.Whitelist.Add(item);
					Configuration.Save();
					ShowPlayer(actor.ObjectId);
					ChatGui.Print($"Whitelist: {actor.Name}{icon}{actor.HomeWorld.GameData.Name} has been added.");
				}
				else
				{
					ChatGui.Print($"Whitelist: {actor.Name}{icon}{actor.HomeWorld.GameData.Name} entry already exists.");
				}
			}
			else
			{
				ChatGui.Print("Whitelist: Invalid target.");
			}
		}

		private void OpenConfigUi()
		{
			_drawConfig = !_drawConfig;
		}

		private void BuildUi()
		{
			_drawConfig = _drawConfig && Configuration.DrawConfigUi();
		}

		private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
		{
			if (!Configuration.Enabled)
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

				if (Configuration.VoidList.Any(x =>
					x.HomeworldId == (isEmoteType ? emotePlayerPayload?.World.RowId : playerPayload?.World.RowId)
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
			_refresh = Configuration.Enabled;
		}
	}
}
