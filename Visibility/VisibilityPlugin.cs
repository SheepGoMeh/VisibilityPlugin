using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using Dalamud.Game.Command;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using Visibility.Configuration;
using Visibility.Utils;
using Visibility.Void;
using XivCommon;

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
		public VisibilityConfiguration PluginConfiguration;

		private bool _drawConfig;
		private bool _refresh;
		public bool Disable;
		public XivCommonBase Common;
		private ContextMenu _contextMenu;
		
		private CharacterDrawResolver _characterDrawResolver;

		public Action<string> Print => s => PluginInterface?.Framework.Gui.Chat.Print(s);

		public void Initialize(DalamudPluginInterface pluginInterface)
		{
			PluginInterface = pluginInterface;
			PluginConfiguration = pluginInterface.GetPluginConfig() as VisibilityConfiguration ?? new VisibilityConfiguration();
			PluginConfiguration.Init(this, pluginInterface);

			PluginInterface.CommandManager.AddHandler(PluginCommandName, new CommandInfo(PluginCommand)
			{
				HelpMessage = $"Shows the config for the visibility plugin.\nAdditional help available via '{PluginCommandName} help'",
				ShowInHelp = true
			});

			PluginInterface.CommandManager.AddHandler(VoidCommandName, new CommandInfo(VoidPlayer)
			{
				HelpMessage = $"Adds player to void list.\nUsage: {VoidCommandName} <firstname> <lastname> <worldname> <reason>",
				ShowInHelp = true
			});
			
			PluginInterface.CommandManager.AddHandler(VoidTargetCommandName, new CommandInfo(VoidTargetPlayer)
			{
				HelpMessage = $"Adds targeted player to void list.\nUsage: {VoidTargetCommandName} <reason>",
				ShowInHelp = true
			});
			
			PluginInterface.CommandManager.AddHandler(WhitelistCommandName, new CommandInfo(WhitelistPlayer)
			{
				HelpMessage = $"Adds player to whitelist.\nUsage: {WhitelistCommandName} <firstname> <lastname> <worldname>",
				ShowInHelp = true
			});
			
			PluginInterface.CommandManager.AddHandler(WhitelistTargetCommandName, new CommandInfo(WhitelistTargetPlayer)
			{
				HelpMessage = $"Adds targeted player to whitelist.\nUsage: {WhitelistTargetCommandName}",
				ShowInHelp = true
			});

			_characterDrawResolver = new CharacterDrawResolver();
			_characterDrawResolver.Init(pluginInterface, PluginConfiguration);

			Common = new XivCommonBase(PluginInterface, Hooks.ContextMenu);
			_contextMenu = new ContextMenu(this);

			PluginInterface.Framework.OnUpdateEvent += FrameworkOnOnUpdateEvent;
			PluginInterface.UiBuilder.OnBuildUi += BuildUi;
			PluginInterface.UiBuilder.OnOpenConfigUi += OpenConfigUi;
			PluginInterface.Framework.Gui.Chat.OnChatMessage += OnChatMessage;
		}

		private void FrameworkOnOnUpdateEvent(Framework framework)
		{
			if (Disable)
			{
				_refresh = false;
				Disable = false;

				_characterDrawResolver.UnhideAll();
			}
			else if (_refresh)
			{
				PluginConfiguration.Enabled = false;
				_characterDrawResolver.UnhideAll();
				_refresh = false;

				Task.Run(async () =>
				{
					await Task.Delay(250);
					PluginConfiguration.Enabled = true;
					Print("Refresh complete.");
				});
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			_contextMenu.Dispose();
			_characterDrawResolver.Dispose();

			PluginInterface.Framework.OnUpdateEvent -= FrameworkOnOnUpdateEvent;
			PluginInterface.UiBuilder.OnBuildUi -= BuildUi;
			PluginInterface.UiBuilder.OnOpenConfigUi -= OpenConfigUi;
			PluginInterface.Framework.Gui.Chat.OnChatMessage -= OnChatMessage;
			PluginInterface.CommandManager.RemoveHandler(PluginCommandName);
			PluginInterface.CommandManager.RemoveHandler(VoidCommandName);
			PluginInterface.CommandManager.RemoveHandler(VoidTargetCommandName);
			PluginInterface.CommandManager.RemoveHandler(WhitelistCommandName);
			PluginInterface.CommandManager.RemoveHandler(WhitelistTargetCommandName);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public void Unhide(UnitType unitType, ContainerType containerType)
		{
			_characterDrawResolver.Unhide(unitType, containerType);
		}

		public void UnhidePlayers(ContainerType type)
		{
			_characterDrawResolver.UnhidePlayers(type);
		}
		
		public void UnhidePets(ContainerType type)
		{
			_characterDrawResolver.UnhidePets(type);
		}

		public void UnhideMinions(ContainerType type)
		{
			_characterDrawResolver.UnhideMinions(type);
		}

		public void UnhideChocobos(ContainerType type)
		{
			_characterDrawResolver.UnhideChocobos(type);
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
					Print($"{PluginCommandName} help - This help menu");
					Print($"{PluginCommandName} refresh - Refreshes hidden actors");
					Print($"{PluginCommandName} <setting> <on/off/toggle> - Sets a setting to on, off or toggles it");
					Print("Available values:");

					foreach (var key in PluginConfiguration.settingDictionary.Keys)
					{
						Print($"{key}");
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
					Print("Too few arguments specified.");
					return;
				}
				
				if (!PluginConfiguration.settingDictionary.Keys.Any(x => x.Equals(args[0], StringComparison.InvariantCultureIgnoreCase)))
				{
					Print($"'{args[0]}' is not a valid value.");
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
						Print($"'{args[1]}' is not a valid value.");
						return;
				}

				PluginConfiguration.settingDictionary[args[0].ToLowerInvariant()].Invoke(value);
				PluginConfiguration.Save();
			}
		}

		public void VoidPlayer(string command, string arguments)
		{
			if (string.IsNullOrEmpty(arguments))
			{
				Print("VoidList: No arguments specified.");
				return;
			}

			var args = arguments.Split(new[] { ' ' }, 4);

			if (args.Length < 3)
			{
				Print("VoidList: Too few arguments specified.");
				return;
			}

			var world = PluginInterface.Data.GetExcelSheet<World>().SingleOrDefault(x =>
				x.DataCenter.Value.Region != 0 &&
				x.Name.ToString().Equals(args[2], StringComparison.InvariantCultureIgnoreCase));

			if (world == default(World))
			{
				Print($"VoidList: '{args[2]}' is not a valid world name.");
				return;
			}

			var playerName = $"{args[0].ToUppercase()} {args[1].ToUppercase()}";

			var voidItem = (!(PluginInterface.ClientState.Actors
				.SingleOrDefault(x => x is PlayerCharacter character
				                      && character.HomeWorld.Id == world.RowId
				                      && character.Name.Equals(playerName, StringComparison.InvariantCultureIgnoreCase)) is PlayerCharacter actor)
				? new VoidItem(playerName, world.Name, world.RowId, args.Length == 3 ? string.Empty : args[3], command == "VoidUIManual")
				: new VoidItem(actor, args.Length == 3 ? string.Empty : args[3], command == "VoidUIManual"));

			var icon = Encoding.UTF8.GetString(new IconPayload(BitmapFontIcon.CrossWorld).Encode()); 

			if (!PluginConfiguration.VoidList.Any(x =>
				(x.Name == voidItem.Name && x.HomeworldId == voidItem.HomeworldId) ||
				(x.ActorId == voidItem.ActorId && x.ActorId != 0)))
			{
				PluginConfiguration.VoidList.Add(voidItem);
				PluginConfiguration.Save();
				Print($"VoidList: {playerName}{icon}{world.Name} has been added.");
			}
			else
			{
				Print($"VoidList: {playerName}{icon}{world.Name} entry already exists.");
			}
		}

		public void VoidTargetPlayer(string command, string arguments)
		{
			if (PluginInterface.ClientState.Actors
				.SingleOrDefault(x => x is PlayerCharacter
				                      && x.ActorId != 0
				                      && x.ActorId != PluginInterface.ClientState.LocalPlayer?.ActorId
				                      && x.ActorId == PluginInterface.ClientState.LocalPlayer?.TargetActorID) is PlayerCharacter actor)
			{
				var voidItem = new VoidItem(actor, arguments, false);
				var icon = Encoding.UTF8.GetString(new byte[] {2, 18, 2, 89, 3});
				
				if (!PluginConfiguration.VoidList.Any(x =>
					(x.Name == voidItem.Name && x.HomeworldId == voidItem.HomeworldId) ||
					(x.ActorId == voidItem.ActorId && x.ActorId != 0)))
				{
					PluginConfiguration.VoidList.Add(voidItem);
					PluginConfiguration.Save();
					Print($"VoidList: {actor.Name}{icon}{actor.HomeWorld.GameData.Name} has been added.");
				}
				else
				{
					Print($"VoidList: {actor.Name}{icon}{actor.HomeWorld.GameData.Name} entry already exists.");
				}
			}
			else
			{
				Print("VoidList: Invalid target.");
			}
		}
		
		public void WhitelistPlayer(string command, string arguments)
		{
			if (string.IsNullOrEmpty(arguments))
			{
				Print("Whitelist: No arguments specified.");
				return;
			}

			var args = arguments.Split(new[] { ' ' }, 4);

			if (args.Length < 3)
			{
				Print("Whitelist: Too few arguments specified.");
				return;
			}

			var world = PluginInterface.Data.GetExcelSheet<World>().SingleOrDefault(x =>
				x.DataCenter.Value.Region != 0 &&
				x.Name.ToString().Equals(args[2], StringComparison.InvariantCultureIgnoreCase));

			if (world == default(World))
			{
				Print($"Whitelist: '{args[2]}' is not a valid world name.");
				return;
			}

			var playerName = $"{args[0].ToUppercase()} {args[1].ToUppercase()}";

			var item = (!(PluginInterface.ClientState.Actors
				.SingleOrDefault(x => x is PlayerCharacter character
				                      && character.HomeWorld.Id == world.RowId
				                      && character.Name.Equals(playerName, StringComparison.InvariantCultureIgnoreCase)) is PlayerCharacter actor)
				? new VoidItem(playerName, world.Name, world.RowId, args.Length == 3 ? string.Empty : args[3], command == "WhitelistUIManual")
				: new VoidItem(actor, args.Length == 3 ? string.Empty : args[3], command == "WhitelistUIManual"));

			var icon = Encoding.UTF8.GetString(new IconPayload(BitmapFontIcon.CrossWorld).Encode()); 

			if (!PluginConfiguration.Whitelist.Any(x =>
				(x.Name == item.Name && x.HomeworldId == item.HomeworldId) ||
				(x.ActorId == item.ActorId && x.ActorId != 0)))
			{
				PluginConfiguration.Whitelist.Add(item);
				PluginConfiguration.Save();
				_characterDrawResolver.UnhidePlayer((uint) item.ActorId);
				Print($"Whitelist: {playerName}{icon}{world.Name} has been added.");
			}
			else
			{
				Print($"Whitelist: {playerName}{icon}{world.Name} entry already exists.");
			}
		}

		public void WhitelistTargetPlayer(string command, string arguments)
		{
			if (PluginInterface.ClientState.Actors
				.SingleOrDefault(x => x is PlayerCharacter
				                      && x.ActorId != 0
				                      && x.ActorId != PluginInterface.ClientState.LocalPlayer?.ActorId
				                      && x.ActorId == PluginInterface.ClientState.LocalPlayer?.TargetActorID) is PlayerCharacter actor)
			{
				var item = new VoidItem(actor, arguments, false);
				var icon = Encoding.UTF8.GetString(new byte[] {2, 18, 2, 89, 3});
				
				if (!PluginConfiguration.Whitelist.Any(x =>
					(x.Name == item.Name && x.HomeworldId == item.HomeworldId) ||
					(x.ActorId == item.ActorId && x.ActorId != 0)))
				{
					PluginConfiguration.Whitelist.Add(item);
					PluginConfiguration.Save();
					_characterDrawResolver.UnhidePlayer((uint) item.ActorId);
					Print($"Whitelist: {actor.Name}{icon}{actor.HomeWorld.GameData.Name} has been added.");
				}
				else
				{
					Print($"Whitelist: {actor.Name}{icon}{actor.HomeWorld.GameData.Name} entry already exists.");
				}
			}
			else
			{
				Print("Whitelist: Invalid target.");
			}
		}

		private void OpenConfigUi(object sender, EventArgs eventArgs)
		{
			_drawConfig = !_drawConfig;
		}

		private void BuildUi()
		{
			_drawConfig = _drawConfig && PluginConfiguration.DrawConfigUi();
		}

		private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
		{
			if (!PluginConfiguration.Enabled)
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

				if (PluginConfiguration.VoidList.Any(x =>
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
			_refresh = PluginConfiguration.Enabled;
		}
	}
}
