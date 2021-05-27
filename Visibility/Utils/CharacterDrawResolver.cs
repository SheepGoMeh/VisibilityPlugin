using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Visibility.Configuration;
using Visibility.Structs.Client.Game.Character;

namespace Visibility.Utils
{
	public enum ContainerType
	{
		Friend,
		Party,
		Company,
		All
	}

	public enum UnitType
	{
		Players,
		Pets,
		Chocobos,
		Minions
	}

	internal class CharacterDrawResolver
	{
		private HashSet<uint> HiddenObjectIds = new HashSet<uint>();
		private HashSet<uint> ObjectIdsToUnhide = new HashSet<uint>();

		#region Players

		private Dictionary<ContainerType, HashSet<uint>> _players = new Dictionary<ContainerType, HashSet<uint>>
		{
			{ContainerType.All, new HashSet<uint>()},
			{ContainerType.Friend, new HashSet<uint>()},
			{ContainerType.Party, new HashSet<uint>()},
			{ContainerType.Company, new HashSet<uint>()},
		};

		#endregion

		#region Pets

		private Dictionary<ContainerType, HashSet<uint>> _pets = new Dictionary<ContainerType, HashSet<uint>>
		{
			{ContainerType.All, new HashSet<uint>()},
			{ContainerType.Friend, new HashSet<uint>()},
			{ContainerType.Party, new HashSet<uint>()},
			{ContainerType.Company, new HashSet<uint>()},
		};

		#endregion

		#region Chocobos

		private Dictionary<ContainerType, HashSet<uint>> _chocobos = new Dictionary<ContainerType, HashSet<uint>>
		{
			{ContainerType.All, new HashSet<uint>()},
			{ContainerType.Friend, new HashSet<uint>()},
			{ContainerType.Party, new HashSet<uint>()},
			{ContainerType.Company, new HashSet<uint>()},
		};

		#endregion

		#region Minions

		private Dictionary<ContainerType, HashSet<uint>> _minions = new Dictionary<ContainerType, HashSet<uint>>
		{
			{ContainerType.All, new HashSet<uint>()},
			{ContainerType.Friend, new HashSet<uint>()},
			{ContainerType.Party, new HashSet<uint>()},
			{ContainerType.Company, new HashSet<uint>()},
		};

		private HashSet<uint> HiddenMinionObjectIds = new HashSet<uint>();
		private HashSet<uint> MinionObjectIdsToUnhide = new HashSet<uint>();

		#endregion

		private unsafe BattleChara* LocalPlayer;

		// void Client::Game::Character::Character::EnableDraw(Client::Game::Character::Character* thisPtr);
		private unsafe delegate void CharacterEnableDrawPrototype(Character* thisPtr);

		// void Client::Game::Character::Character::DisableDraw(Client::Game::Character::Character* thisPtr);
		private unsafe delegate void CharacterDisableDrawPrototype(Character* thisPtr);

		// void Client::Game::Character::Companion::EnableDraw(Client::Game::Character::Companion* thisPtr);
		private unsafe delegate void CompanionEnableDrawPrototype(Companion* thisPtr);

		// void dtor_Client::Game::Character::Character(Client::Game::Character::Character* thisPtr);
		private unsafe delegate void CharacterDtorPrototype(Character* thisPtr);

		private Hook<CharacterEnableDrawPrototype> hookCharacterEnableDraw;
		private Hook<CharacterDisableDrawPrototype> hookCharacterDisableDraw;
		private Hook<CompanionEnableDrawPrototype> hookCompanionEnableDraw;
		private Hook<CharacterDtorPrototype> hookCharacterDtor;

		private readonly AddressResolver _address = new AddressResolver();
		private VisibilityConfiguration _config;
		private DalamudPluginInterface _pluginInterface;

		public unsafe void Init(DalamudPluginInterface pluginInterface, VisibilityConfiguration pluginConfig)
		{
			_pluginInterface = pluginInterface;
			_address.Setup(pluginInterface.TargetModuleScanner);
			_config = pluginConfig;

			LocalPlayer = *(BattleChara**)_address.LocalPlayerAddress.ToPointer();

			hookCharacterEnableDraw = new Hook<CharacterEnableDrawPrototype>(_address.CharacterEnableDrawAddress, new CharacterEnableDrawPrototype(CharacterEnableDrawDetour), this);
			hookCharacterDisableDraw = new Hook<CharacterDisableDrawPrototype>(_address.CharacterDisableDrawAddress, new CharacterDisableDrawPrototype(CharacterDisableDrawDetour), this);
			hookCompanionEnableDraw = new Hook<CompanionEnableDrawPrototype>(_address.CompanionEnableDrawAddress, new CompanionEnableDrawPrototype(CompanionEnableDrawDetour), this);
			hookCharacterDtor = new Hook<CharacterDtorPrototype>(_address.CharacterDtorAddress, new CharacterDtorPrototype(CharacterDtorDetour), this);

			hookCharacterEnableDraw.Enable();
			hookCharacterDisableDraw.Enable();
			hookCompanionEnableDraw.Enable();
			hookCharacterDtor.Enable();
		}

		public void Unhide(UnitType unitType, ContainerType containerType)
		{
			HashSet<uint> container;

			switch (unitType)
			{
				case UnitType.Players:
					container = _players[containerType];
					break;
				case UnitType.Pets:
					container = _pets[containerType];
					break;
				case UnitType.Chocobos:
					container = _chocobos[containerType];
					break;
				case UnitType.Minions:
					container = _minions[containerType];
					break;
				default:
					return;
			}

			ObjectIdsToUnhide.UnionWith(container);
			HiddenObjectIds.ExceptWith(container);
		}

		public void UnhidePlayers(ContainerType type)
		{
			ObjectIdsToUnhide.UnionWith(_players[type]);
			HiddenObjectIds.ExceptWith(_players[type]);
		}

		public void UnhidePets(ContainerType type)
		{
			ObjectIdsToUnhide.UnionWith(_pets[type]);
			HiddenObjectIds.ExceptWith(_pets[type]);
		}

		public void UnhideChocobos(ContainerType type)
		{
			ObjectIdsToUnhide.UnionWith(_chocobos[type]);
			HiddenObjectIds.ExceptWith(_chocobos[type]);
		}

		public void UnhideMinions(ContainerType type)
		{
			MinionObjectIdsToUnhide.UnionWith(_minions[type]);
			HiddenMinionObjectIds.ExceptWith(_minions[type]);
		}

		public unsafe void UnhideAll()
		{
			foreach (var actor in _pluginInterface.ClientState.Actors)
			{
				var thisPtr = (Character*) actor.Address;

				if (thisPtr->GameObject.ObjectKind == (byte) ObjectKind.Companion)
				{
					if (!HiddenMinionObjectIds.Contains(thisPtr->CompanionOwnerID))
					{
						continue;
					}
					
					thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
					MinionObjectIdsToUnhide.Remove(thisPtr->CompanionOwnerID);
				}
				else
				{
					if (!HiddenObjectIds.Contains(thisPtr->GameObject.ObjectID))
					{
						continue;
					}
					
					thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
					HiddenObjectIds.Remove(thisPtr->GameObject.ObjectID);
				}
			}
		}

		public void UnhidePlayer(uint id)
		{
			if (!HiddenObjectIds.Contains(id))
			{
				return;
			}

			ObjectIdsToUnhide.Add(id);
			HiddenObjectIds.Remove(id);
		}

		public unsafe void UpdateLocalPlayer()
		{
			LocalPlayer = *(BattleChara**)_address.LocalPlayerAddress.ToPointer();
		}

		private static unsafe bool UnsafeArrayEqual(byte* arr1, byte* arr2, int len)
		{
			while (len-- != 0)
			{
				if (*arr1++ != *arr2++)
				{
					return false;
				}
			}

			return true;
		}

		private static unsafe bool UnsafeArrayEqual(IReadOnlyList<byte> arr1, byte* arr2, int len)
		{
			for (var i = 0; i != len; ++i)
			{
				if (arr1[i] != *arr2++)
				{
					return false;
				}
			}

			return true;
		}

		private unsafe void CharacterEnableDrawDetour(Character* thisPtr)
		{
			var localPlayerAddress = _pluginInterface?.ClientState?.LocalPlayer?.Address;
			
			if (localPlayerAddress.HasValue && LocalPlayer != (BattleChara*)localPlayerAddress.Value)
			{
				LocalPlayer = (BattleChara*)localPlayerAddress.Value;
			}
			
			if (_config.Enabled && localPlayerAddress.HasValue && thisPtr != (Character*)LocalPlayer)
			{
				switch (thisPtr->GameObject.ObjectKind)
				{
					case (byte)ObjectKind.Player:
						if (thisPtr->GameObject.ObjectID == 0xE0000000)
						{
							break;
						}

						_players[ContainerType.All].Add(thisPtr->GameObject.ObjectID);

						if ((thisPtr->StatusFlags & (byte)StatusFlags.Friend) > 0)
						{
							_players[ContainerType.Friend].Add(thisPtr->GameObject.ObjectID);
						}
						else
						{
							_players[ContainerType.Friend].Remove(thisPtr->GameObject.ObjectID);
						}

						if ((thisPtr->StatusFlags & (byte)StatusFlags.PartyMember) > 0)
						{
							_players[ContainerType.Party].Add(thisPtr->GameObject.ObjectID);
						}
						else
						{
							_players[ContainerType.Party].Remove(thisPtr->GameObject.ObjectID);
						}

						if ((_pluginInterface.ClientState.Condition[ConditionFlag.BoundByDuty]
							|| _pluginInterface.ClientState.Condition[ConditionFlag.BetweenAreas]
							|| _pluginInterface.ClientState.Condition[ConditionFlag.WatchingCutscene])
							&& !_config.TerritoryTypeWhitelist.Contains(_pluginInterface.ClientState.TerritoryType))
						{
							break;
						}

						if (*LocalPlayer->Character.CompanyTag != 0
							&& LocalPlayer->Character.CurrentWorld == LocalPlayer->Character.HomeWorld
							&& UnsafeArrayEqual(thisPtr->CompanyTag, LocalPlayer->Character.CompanyTag, 7))
						{
							_players[ContainerType.Company].Add(thisPtr->GameObject.ObjectID);
						}
						else
						{
							_players[ContainerType.Company].Remove(thisPtr->GameObject.ObjectID);
						}

						if (_config.VoidList.Any(x => UnsafeArrayEqual(x.NameBytes,
							                              thisPtr->GameObject.Name,
							                              x.NameBytes.Length) &&
						                              x.HomeworldId == thisPtr->HomeWorld))
						{
							thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
							HiddenObjectIds.Add(thisPtr->GameObject.ObjectID);
							break;
						}

						if (!_config.HidePlayer ||
							(_config.ShowDeadPlayer && thisPtr->CurrentHp == 0) ||
							(_config.ShowFriendPlayer && _players[ContainerType.Friend].Contains(thisPtr->GameObject.ObjectID)) ||
							(_config.ShowCompanyPlayer && _players[ContainerType.Company].Contains(thisPtr->GameObject.ObjectID)) ||
							(_config.ShowPartyPlayer && _players[ContainerType.Party].Contains(thisPtr->GameObject.ObjectID)) ||
							(_config.Whitelist.Any(x => UnsafeArrayEqual(x.NameBytes,
								                            thisPtr->GameObject.Name,
								                            x.NameBytes.Length) &&
							                            x.HomeworldId == thisPtr->HomeWorld)))
						{
							break;
						}
						else
						{
							thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
							HiddenObjectIds.Add(thisPtr->GameObject.ObjectID);
							break;
						}
					case (byte)ObjectKind.BattleNpc when thisPtr->GameObject.SubKind == (byte)BattleNpcSubKind.Pet && thisPtr->NameID != 6565:
						if (!_config.HidePet
							|| thisPtr->GameObject.OwnerID == LocalPlayer->Character.GameObject.ObjectID)
						{
							break;
						}

						_pets[ContainerType.All].Add(thisPtr->GameObject.ObjectID);

						if (_players[ContainerType.Friend].Contains(thisPtr->GameObject.OwnerID))
						{
							_pets[ContainerType.Friend].Add(thisPtr->GameObject.ObjectID);
						}

						if (_players[ContainerType.Party].Contains(thisPtr->GameObject.OwnerID))
						{
							_pets[ContainerType.Party].Add(thisPtr->GameObject.ObjectID);
						}

						if (_players[ContainerType.Company].Contains(thisPtr->GameObject.OwnerID))
						{
							_pets[ContainerType.Company].Add(thisPtr->GameObject.ObjectID);
						}

						if ((_config.ShowFriendPet && _players[ContainerType.Friend].Contains(thisPtr->GameObject.OwnerID))
							|| (_config.ShowCompanyPet && _players[ContainerType.Company].Contains(thisPtr->GameObject.OwnerID))
							|| (_config.ShowPartyPet && _players[ContainerType.Party].Contains(thisPtr->GameObject.OwnerID)))
						{
							break;
						}
						else
						{
							thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
							HiddenObjectIds.Add(thisPtr->GameObject.ObjectID);
							break;
						}
					case (byte) ObjectKind.BattleNpc
						when thisPtr->GameObject.SubKind == (byte) BattleNpcSubKind.Pet && thisPtr->NameID == 6565
						: // Earthly Star
						if (_config.HideStar
						    && _pluginInterface.ClientState.Condition[ConditionFlag.InCombat]
						    && thisPtr->GameObject.OwnerID != LocalPlayer->Character.GameObject.ObjectID
						    && !_players[ContainerType.Party].Contains(thisPtr->GameObject.OwnerID))
						{
							thisPtr->GameObject.RenderFlags |= (int) VisibilityFlags.Invisible;
							HiddenObjectIds.Add(thisPtr->GameObject.ObjectID);
						}

						break;
					case (byte)ObjectKind.BattleNpc when thisPtr->GameObject.SubKind == 3:
						if (!_config.HideChocobo
							|| thisPtr->GameObject.OwnerID == LocalPlayer->Character.GameObject.ObjectID)
						{
							break;
						}

						_chocobos[ContainerType.All].Add(thisPtr->GameObject.ObjectID);

						if (_players[ContainerType.Friend].Contains(thisPtr->GameObject.OwnerID))
						{
							_chocobos[ContainerType.Friend].Add(thisPtr->GameObject.ObjectID);
						}

						if (_players[ContainerType.Party].Contains(thisPtr->GameObject.OwnerID))
						{
							_chocobos[ContainerType.Party].Add(thisPtr->GameObject.ObjectID);
						}

						if (_players[ContainerType.Company].Contains(thisPtr->GameObject.OwnerID))
						{
							_chocobos[ContainerType.Company].Add(thisPtr->GameObject.ObjectID);
						}

						if ((_config.ShowFriendChocobo && _players[ContainerType.Friend].Contains(thisPtr->GameObject.OwnerID))
							|| (_config.ShowCompanyChocobo && _players[ContainerType.Company].Contains(thisPtr->GameObject.OwnerID))
							|| (_config.ShowPartyChocobo && _players[ContainerType.Party].Contains(thisPtr->GameObject.OwnerID)))
						{
							break;
						}
						else
						{
							thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
							HiddenObjectIds.Add(thisPtr->GameObject.ObjectID);
							break;
						}
				}
			}

			hookCharacterEnableDraw.Original(thisPtr);
		}

		private unsafe void CharacterDisableDrawDetour(Character* thisPtr)
		{
			if (thisPtr->GameObject.ObjectKind == (byte)ObjectKind.Player
				&& (thisPtr->StatusFlags & (byte)StatusFlags.PartyMember) > 0)
			{
				_players[ContainerType.Party].Add(thisPtr->GameObject.ObjectID);
				thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
				HiddenObjectIds.Remove(thisPtr->GameObject.ObjectID);
			}

			if (_config.HidePlayer
				&& _config.ShowDeadPlayer
				&& thisPtr->GameObject.ObjectKind == (byte)ObjectKind.Player
				&& thisPtr->CurrentHp == 0
				&& HiddenObjectIds.Contains(thisPtr->GameObject.ObjectID))
			{
				thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
				HiddenObjectIds.Remove(thisPtr->GameObject.ObjectID);
			}
			else if (ObjectIdsToUnhide.Contains(thisPtr->GameObject.ObjectID))
			{
				thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
				ObjectIdsToUnhide.Remove(thisPtr->GameObject.ObjectID);
			}
			else if (MinionObjectIdsToUnhide.Contains(thisPtr->CompanionOwnerID))
			{
				thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
				MinionObjectIdsToUnhide.Remove(thisPtr->CompanionOwnerID);
			}

			hookCharacterDisableDraw.Original(thisPtr);
		}

		private unsafe void CompanionEnableDrawDetour(Companion* thisPtr)
		{
			if (_config.Enabled
				&& _config.HideMinion
				&& thisPtr->Character.CompanionOwnerID != LocalPlayer->Character.GameObject.ObjectID)
			{
				_minions[ContainerType.All].Add(thisPtr->Character.CompanionOwnerID);

				if (_players[ContainerType.Friend].Contains(thisPtr->Character.CompanionOwnerID))
				{
					_minions[ContainerType.Friend].Add(thisPtr->Character.CompanionOwnerID);
				}

				if (_players[ContainerType.Party].Contains(thisPtr->Character.CompanionOwnerID))
				{
					_minions[ContainerType.Party].Add(thisPtr->Character.CompanionOwnerID);
				}

				if (_players[ContainerType.Company].Contains(thisPtr->Character.CompanionOwnerID))
				{
					_minions[ContainerType.Company].Add(thisPtr->Character.CompanionOwnerID);
				}

				if ((_config.ShowFriendMinion && _players[ContainerType.Friend].Contains(thisPtr->Character.CompanionOwnerID))
					|| (_config.ShowCompanyMinion && _players[ContainerType.Company].Contains(thisPtr->Character.CompanionOwnerID))
					|| (_config.ShowPartyMinion && _players[ContainerType.Party].Contains(thisPtr->Character.CompanionOwnerID)))
				{
				}
				else
				{
					thisPtr->Character.GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
					HiddenMinionObjectIds.Add(thisPtr->Character.CompanionOwnerID);
				}
			}

			hookCompanionEnableDraw.Original(thisPtr);
		}

		private unsafe void CharacterDtorDetour(Character* thisPtr)
		{
			if (thisPtr->GameObject.ObjectKind == (byte)ObjectKind.Player
				|| thisPtr->GameObject.ObjectKind == (byte)ObjectKind.Companion
				|| (thisPtr->GameObject.ObjectKind == (byte)ObjectKind.BattleNpc
					&& (thisPtr->GameObject.SubKind == (byte)BattleNpcSubKind.Pet ||
						thisPtr->GameObject.SubKind == 3)))
			{
				HiddenObjectIds.Remove(thisPtr->GameObject.ObjectID);
				HiddenMinionObjectIds.Remove(thisPtr->GameObject.ObjectID);

				foreach (ContainerType type in Enum.GetValues(typeof(ContainerType)))
				{
					_players[type].Remove(thisPtr->GameObject.ObjectID);
					_pets[type].Remove(thisPtr->GameObject.ObjectID);
					_chocobos[type].Remove(thisPtr->GameObject.ObjectID);
					_minions[type].Remove(thisPtr->GameObject.ObjectID);
				}
			}

			hookCharacterDtor.Original(thisPtr);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			hookCharacterEnableDraw.Disable();
			hookCharacterDisableDraw.Disable();
			hookCompanionEnableDraw.Disable();
			hookCharacterDtor.Dispose();

			hookCharacterEnableDraw.Dispose();
			hookCharacterDisableDraw.Dispose();
			hookCompanionEnableDraw.Dispose();
			hookCharacterDtor.Dispose();
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}