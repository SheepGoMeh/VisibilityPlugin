using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
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

namespace Visibility
{
	public class VisibilityPlugin : IDalamudPlugin
	{
		public string Name => "Visibility";
		private static string PluginCommandName => "/pvis";
		private static string VoidCommandName => "/void";
		private static string VoidTargetCommandName => "/voidtarget";

		private DalamudPluginInterface _pluginInterface;
		private VisibilityConfiguration _pluginConfig;

		private bool _drawConfig;
		
		private CharacterDrawResolver _characterDrawResolver;

		private Action<string> Print => s => _pluginInterface?.Framework.Gui.Chat.Print(s);

		public void Initialize(DalamudPluginInterface pluginInterface)
		{
			_pluginInterface = pluginInterface;
			_pluginConfig = pluginInterface.GetPluginConfig() as VisibilityConfiguration ?? new VisibilityConfiguration();
			_pluginConfig.Init(this, pluginInterface);

			_pluginInterface.CommandManager.AddHandler(PluginCommandName, new CommandInfo(PluginCommand)
			{
				HelpMessage = $"Shows the config for the visibility plugin.\nAdditional help available via '{PluginCommandName} help'",
				ShowInHelp = true
			});

			_pluginInterface.CommandManager.AddHandler(VoidCommandName, new CommandInfo(VoidPlayer)
			{
				HelpMessage = $"Adds player to void list.\nUsage: {VoidCommandName} <firstname> <lastname> <worldname> <reason>",
				ShowInHelp = true
			});
			
			_pluginInterface.CommandManager.AddHandler(VoidTargetCommandName, new CommandInfo(VoidTargetPlayer)
			{
				HelpMessage = $"Adds targeted player to void list.\nUsage: {VoidTargetCommandName} <reason>",
				ShowInHelp = true
			});

			_characterDrawResolver = new CharacterDrawResolver();
			_characterDrawResolver.Init(pluginInterface, _pluginConfig);

			_pluginInterface.UiBuilder.OnBuildUi += BuildUi;
			_pluginInterface.UiBuilder.OnOpenConfigUi += OpenConfigUi;
			_pluginInterface.Framework.Gui.Chat.OnChatMessage += OnChatMessage;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			_characterDrawResolver.Dispose();

			_pluginInterface.UiBuilder.OnBuildUi -= BuildUi;
			_pluginInterface.UiBuilder.OnOpenConfigUi -= OpenConfigUi;
			_pluginInterface.Framework.Gui.Chat.OnChatMessage -= OnChatMessage;
			_pluginInterface.CommandManager.RemoveHandler(PluginCommandName);
			_pluginInterface.CommandManager.RemoveHandler(VoidCommandName);
			_pluginInterface.CommandManager.RemoveHandler(VoidTargetCommandName);
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
					Print($"{PluginCommandName} <setting> <on/off/toggle> - Sets a setting to on, off or toggles it");
					Print("Available values:");

					foreach (var key in _pluginConfig.settingDictionary.Keys)
					{
						Print($"{key}");
					}
					
					return;
				}

				if (args.Length != 2)
				{
					Print("Too few arguments specified.");
					return;
				}
				
				if (!_pluginConfig.settingDictionary.Keys.Any(x => x.Equals(args[0], StringComparison.InvariantCultureIgnoreCase)))
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

				_pluginConfig.settingDictionary[args[0].ToLowerInvariant()].Invoke(value);
				_pluginConfig.Save();
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

			var dataCenter = _pluginInterface.ClientState.LocalPlayer?.HomeWorld.GameData.DataCenter;

			var world = _pluginInterface.Data.GetExcelSheet<World>()
				.SingleOrDefault(x => x.DataCenter.Row == dataCenter?.Row && x.Name.ToString().Equals(args[2], StringComparison.InvariantCultureIgnoreCase));

			if (world == default(World))
			{
				Print($"VoidList: '{args[2]}' is not a valid world name.");
				return;
			}

			var playerName = $"{args[0].ToUppercase()} {args[1].ToUppercase()}";

			var voidItem = (!(_pluginInterface.ClientState.Actors
				.SingleOrDefault(x => x is PlayerCharacter character
				                      && character.HomeWorld.Id == world.RowId
				                      && character.Name.Equals(playerName, StringComparison.InvariantCultureIgnoreCase)) is PlayerCharacter actor)
				? new VoidItem(playerName, world.Name, world.RowId, args.Length == 3 ? string.Empty : args[3], command == "VoidUIManual")
				: new VoidItem(actor, args[3], command == "VoidUIManual"));

			var icon = Encoding.UTF8.GetString(new byte[] {2, 18, 2, 89, 3});

			if (!_pluginConfig.VoidList.Any(x =>
				(x.Name == voidItem.Name && x.HomeworldId == voidItem.HomeworldId) ||
				(x.ActorId == voidItem.ActorId && x.ActorId != 0)))
			{
				_pluginConfig.VoidList.Add(voidItem);
				_pluginConfig.Save();
				Print($"VoidList: {playerName}{icon}{world.Name} has been added to the void list.");
			}
			else
			{
				Print($"VoidList: {playerName}{icon}{world.Name} entry already exists.");
			}
		}

		public void VoidTargetPlayer(string command, string arguments)
		{
			if (_pluginInterface.ClientState.Actors
				.SingleOrDefault(x => x is PlayerCharacter
				                      && x.ActorId != 0
				                      && x.ActorId != _pluginInterface.ClientState.LocalPlayer?.ActorId
				                      && x.ActorId == _pluginInterface.ClientState.LocalPlayer?.TargetActorID) is PlayerCharacter actor)
			{
				var voidItem = new VoidItem(actor, arguments, false);
				var icon = Encoding.UTF8.GetString(new byte[] {2, 18, 2, 89, 3});
				
				if (!_pluginConfig.VoidList.Any(x =>
					(x.Name == voidItem.Name && x.HomeworldId == voidItem.HomeworldId) ||
					(x.ActorId == voidItem.ActorId && x.ActorId != 0)))
				{
					_pluginConfig.VoidList.Add(voidItem);
					_pluginConfig.Save();
					Print($"VoidList: {actor.Name}{icon}{actor.HomeWorld.GameData.Name} has been added to the void list.");
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

		private void OpenConfigUi(object sender, EventArgs eventArgs)
		{
			_drawConfig = !_drawConfig;
		}

		private void BuildUi()
		{
			_drawConfig = _drawConfig && _pluginConfig.DrawConfigUi();
		}

		private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
		{
			if (!_pluginConfig.Enabled) return;
			try
			{
				if (isHandled || !(sender.Payloads.SingleOrDefault(x => x.Type == PayloadType.Player) is PlayerPayload playerPayload))
				{
					return;
				}

				if (_pluginConfig.VoidList.Any(x =>
					x.HomeworldId == playerPayload.World.RowId 
					&& x.Name == playerPayload.PlayerName))
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
			_characterDrawResolver.UnhideAll();

			return;
			/*
			var actors = from actor in _pluginInterface.ClientState.Actors
						 where !(actor is BattleNpc)
						 select actor;

			var battleActors = from actor in _pluginInterface.ClientState.Actors
							   where actor is BattleNpc npc
							   && npc.BattleNpcKind != BattleNpcSubKind.Enemy
							   && npc.OwnerId != _pluginInterface.ClientState.LocalPlayer?.ActorId
							   select actor as BattleNpc;

			foreach (var actor in actors)
			{
				actor.Rerender();
			}

			foreach (var actor in battleActors)
			{
				actor.Rerender();
			}*/
		}
	}
}
