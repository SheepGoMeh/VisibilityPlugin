using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
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
		public bool enabled;
		
		private readonly bool[] _oneShot = { false, false, false, false };
		private readonly int[] _partyActorId = new int[7];

		private PlaceholderResolver _placeholderResolver;

		private Action<string> Print => s => _pluginInterface?.Framework.Gui.Chat.Print(s);

		public void Initialize(DalamudPluginInterface pluginInterface)
		{
			_pluginInterface = pluginInterface;
			_pluginConfig = pluginInterface.GetPluginConfig() as VisibilityConfiguration ?? new VisibilityConfiguration();
			_pluginConfig.Init(this, pluginInterface);

			enabled = _pluginConfig.Enabled;

			pluginInterface.ClientState.OnLogout += (s, e) => enabled = false;
			pluginInterface.ClientState.OnLogin += (s, e) => enabled = _pluginConfig.Enabled;

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

			_placeholderResolver = new PlaceholderResolver();
			_placeholderResolver.Init(pluginInterface);

			_pluginInterface.UiBuilder.OnBuildUi += BuildUi;
			_pluginInterface.Framework.OnUpdateEvent += OnUpdateEvent;
			_pluginInterface.Framework.Gui.Chat.OnChatMessage += OnChatMessage;
		}

		public void Dispose()
		{
			_pluginInterface.UiBuilder.OnBuildUi -= BuildUi;
			_pluginInterface.Framework.OnUpdateEvent -= OnUpdateEvent;
			_pluginInterface.Framework.Gui.Chat.OnChatMessage -= OnChatMessage;
			_pluginInterface.CommandManager.RemoveHandler(PluginCommandName);
			_pluginInterface.CommandManager.RemoveHandler(VoidCommandName);
			_pluginInterface.CommandManager.RemoveHandler(VoidTargetCommandName);
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
					Print($"{PluginCommandName} <setting> <on/off> - Sets a setting to on or off");
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

				bool value;

				if (args[1].Equals("on", StringComparison.InvariantCultureIgnoreCase) ||
				    args[1].Equals("true", StringComparison.InvariantCultureIgnoreCase) || args[1].Equals("1"))
				{
					value = true;
				}
				else if (args[1].Equals("off", StringComparison.InvariantCultureIgnoreCase) ||
				         args[1].Equals("false", StringComparison.InvariantCultureIgnoreCase) || args[1].Equals("0"))
				{
					value = false;
				}
				else
				{
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

			var world = _pluginInterface.Data.GetExcelSheet<World>()
				.SingleOrDefault(x => x.Name.Equals(args[2], StringComparison.InvariantCultureIgnoreCase));

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

			if (!_pluginConfig.VoidList.Any(x =>
				(x.Name == voidItem.Name && x.HomeworldId == voidItem.HomeworldId) ||
				(x.ActorId == voidItem.ActorId && x.ActorId != 0)))
			{
				_pluginConfig.VoidList.Add(voidItem);
				_pluginConfig.Save();
				Print($"VoidList: {playerName}@{world.Name} has been added to the void list.");
			}
			else
			{
				Print($"VoidList: {playerName}@{world.Name} entry already exists.");
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
				
				if (!_pluginConfig.VoidList.Any(x =>
					(x.Name == voidItem.Name && x.HomeworldId == voidItem.HomeworldId) ||
					(x.ActorId == voidItem.ActorId && x.ActorId != 0)))
				{
					_pluginConfig.VoidList.Add(voidItem);
					_pluginConfig.Save();
					Print($"VoidList: {actor.Name}@{actor.HomeWorld.GameData.Name} has been added to the void list.");
				}
				else
				{
					Print($"VoidList: {actor.Name}@{actor.HomeWorld.GameData.Name} entry already exists.");
				}
			}
			else
			{
				Print("VoidList: Invalid target.");
			}
		}

		private void BuildUi()
		{
			_drawConfig = _drawConfig && _pluginConfig.DrawConfigUi();
		}

		private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
		{
			if (!enabled) return;
			try
			{
				if (isHandled) return;
				var senderName = sender.TextValue;

				if (_pluginConfig.VoidList.SingleOrDefault(
					x => x.ActorId != 0
					     && x.ActorId != _pluginInterface.ClientState.LocalPlayer?.ActorId
					     && x.Name != _pluginInterface.ClientState.LocalPlayer?.Name
					     && x.Name == senderName) != null)
				{
					isHandled = true;
				}
			}
			catch (Exception)
			{
				// Ignore exception
			}
		}

		private void CheckRender<T>(IEnumerable<T> collection, ICollection<int> friendCollection,
			ICollection<int> companyCollection, ref bool oneShot, bool hide = false, bool showParty = false,
			bool showFriend = false, bool showCompany = false, bool showDead = false)
			where T : Actor
		{
			if (hide)
			{
				foreach (var item in collection)
				{
					if (item == _pluginInterface.ClientState.LocalPlayer)
					{
						continue;
					}

					var lookupId = item is BattleNpc battleNpc ? battleNpc.OwnerId : item.ActorId;
					if ((showParty && _partyActorId.Contains(lookupId))
					    || (showFriend && friendCollection.Contains(lookupId))
					    || (showCompany && companyCollection.Contains(lookupId))
					    || (showDead && (item as Chara).CurrentHp == 0))
					{
						item.Render();
					}
					else
					{
						item.Hide();
					}
				}


				oneShot = true;
			}
			else if (oneShot)
			{
				foreach (var item in collection)
				{
					item.Render();
				}

				oneShot = false;
			}
		}

		private void OnUpdateEvent(Framework framework)
		{
			if (!enabled ||
			    (_pluginInterface.ClientState.Condition[ConditionFlag.BetweenAreas] ||
			     _pluginInterface.ClientState.Condition[ConditionFlag.BetweenAreas51]))
			{
				return;
			}
			
			for (var i = 0; i != _partyActorId.Length; ++i)
			{
				_partyActorId[i] = _placeholderResolver.GetTargetActorId($"<{i + 2}>");
			}

			var voidItems = from item in _pluginConfig.VoidList
				where item.ActorId == 0
				select item;

			var voidedPlayers = from actor in _pluginInterface.ClientState.Actors
				where _pluginConfig.VoidList.SingleOrDefault(x => x.ActorId != 0 && x.ActorId == actor.ActorId) != null
				      && actor.ActorId != _pluginInterface.ClientState.LocalPlayer?.ActorId
				select actor;

			var players = from actor in _pluginInterface.ClientState.Actors
				where actor is PlayerCharacter character
				      && actor.ActorId != _pluginInterface.ClientState.LocalPlayer?.ActorId
				      && character.HomeWorld.Id != ushort.MaxValue
				      && character.CurrentWorld.Id != ushort.MaxValue
				      && !voidedPlayers.Contains(actor)
				select actor as PlayerCharacter;

			var friends = (from actor in players
				where actor.IsStatus(StatusFlags.Friend)
				select actor.ActorId).ToHashSet();

			var companyMembers = (from actor in _pluginInterface.ClientState.Actors
				where actor is PlayerCharacter playerCharacter
				      && !string.IsNullOrEmpty(playerCharacter.CompanyTag)
				      && playerCharacter.CompanyTag == _pluginInterface.ClientState.LocalPlayer?.CompanyTag
				select actor.ActorId).ToHashSet();

			var pets = from actor in _pluginInterface.ClientState.Actors
				where actor is BattleNpc npc
				      && npc.BattleNpcKind == BattleNpcSubKind.Pet
				      && npc.OwnerId != _pluginInterface.ClientState.LocalPlayer?.ActorId
				select actor as BattleNpc;

			var chocobos = from actor in _pluginInterface.ClientState.Actors
				where actor is BattleNpc npc
				      && (byte)npc.BattleNpcKind == 3
				      && npc.OwnerId != _pluginInterface.ClientState.LocalPlayer?.ActorId
				select actor as BattleNpc;

			CheckRender(chocobos, friends, companyMembers,
				ref _oneShot[0], _pluginConfig.HideChocobo,
				_pluginConfig.ShowPartyChocobo,
				_pluginConfig.ShowFriendChocobo,
				_pluginConfig.ShowCompanyChocobo);

			CheckRender(pets, friends, companyMembers,
				ref _oneShot[1], _pluginConfig.HidePet,
				_pluginConfig.ShowPartyPet,
				_pluginConfig.ShowFriendPet,
				_pluginConfig.ShowCompanyPet);

			if (!_pluginInterface.ClientState.Condition[ConditionFlag.BoundByDuty])
			{
				foreach (var item in voidedPlayers)
				{
					item.Hide();
				}

				CheckRender(players, friends, companyMembers,
					ref _oneShot[2], _pluginConfig.HidePlayer,
					_pluginConfig.ShowPartyPlayer,
					_pluginConfig.ShowFriendPlayer,
					_pluginConfig.ShowCompanyPlayer,
					_pluginConfig.ShowDeadPlayer);
			}

			foreach (var item in voidItems)
			{
				if (!(players.SingleOrDefault(x => x.HomeWorld.Id == item.HomeworldId && x.Name == item.Name) is
					PlayerCharacter player))
				{
					continue;
				}
				
				item.ActorId = player.ActorId;
				_pluginConfig.Save();
			}
		}

		public void RefreshActors()
		{
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
			}
		}
	}
}
