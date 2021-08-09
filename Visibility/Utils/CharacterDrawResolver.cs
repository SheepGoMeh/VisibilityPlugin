using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Visibility.Configuration;

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

		private readonly Dictionary<UnitType, Dictionary<ContainerType, HashSet<uint>>> _containers = new()
		{
			{
				UnitType.Players, new Dictionary<ContainerType, HashSet<uint>>
				{
					{ContainerType.All, new HashSet<uint>()},
					{ContainerType.Friend, new HashSet<uint>()},
					{ContainerType.Party, new HashSet<uint>()},
					{ContainerType.Company, new HashSet<uint>()},
				}
			},
			{
				UnitType.Pets, new Dictionary<ContainerType, HashSet<uint>>
				{
					{ContainerType.All, new HashSet<uint>()},
					{ContainerType.Friend, new HashSet<uint>()},
					{ContainerType.Party, new HashSet<uint>()},
					{ContainerType.Company, new HashSet<uint>()},
				}
			},
			{
				UnitType.Chocobos, new Dictionary<ContainerType, HashSet<uint>>
				{
					{ContainerType.All, new HashSet<uint>()},
					{ContainerType.Friend, new HashSet<uint>()},
					{ContainerType.Party, new HashSet<uint>()},
					{ContainerType.Company, new HashSet<uint>()},
				}
			},
			{
				UnitType.Minions, new Dictionary<ContainerType, HashSet<uint>>
				{
					{ContainerType.All, new HashSet<uint>()},
					{ContainerType.Friend, new HashSet<uint>()},
					{ContainerType.Party, new HashSet<uint>()},
					{ContainerType.Company, new HashSet<uint>()},
				}
			},
		};

		private HashSet<uint> HiddenMinionObjectIds = new HashSet<uint>();
		private HashSet<uint> MinionObjectIdsToUnhide = new HashSet<uint>();

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

			hookCharacterEnableDraw = new Hook<CharacterEnableDrawPrototype>(_address.CharacterEnableDrawAddress, CharacterEnableDrawDetour);
			hookCharacterDisableDraw = new Hook<CharacterDisableDrawPrototype>(_address.CharacterDisableDrawAddress, CharacterDisableDrawDetour);
			hookCompanionEnableDraw = new Hook<CompanionEnableDrawPrototype>(_address.CompanionEnableDrawAddress, CompanionEnableDrawDetour);
			hookCharacterDtor = new Hook<CharacterDtorPrototype>(_address.CharacterDtorAddress, CharacterDtorDetour);

			hookCharacterEnableDraw.Enable();
			hookCharacterDisableDraw.Enable();
			hookCompanionEnableDraw.Enable();
			hookCharacterDtor.Enable();
		}

		public void Unhide(UnitType unitType, ContainerType containerType)
		{
			ObjectIdsToUnhide.UnionWith(_containers[unitType][containerType]);
			HiddenObjectIds.ExceptWith(_containers[unitType][containerType]);
		}

		public void UnhidePlayers(ContainerType type) => Unhide(UnitType.Players, type);

		public void UnhidePets(ContainerType type) => Unhide(UnitType.Pets, type);

		public void UnhideChocobos(ContainerType type) => Unhide(UnitType.Chocobos, type);

		public void UnhideMinions(ContainerType type) => Unhide(UnitType.Minions, type);

		public unsafe void UnhideAll()
		{
			foreach (var actor in _pluginInterface.ClientState.Objects)
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

						_containers[UnitType.Players][ContainerType.All].Add(thisPtr->GameObject.ObjectID);

						if ((thisPtr->StatusFlags & (byte)StatusFlags.Friend) > 0)
						{
							_containers[UnitType.Players][ContainerType.Friend].Add(thisPtr->GameObject.ObjectID);
						}
						else
						{
							_containers[UnitType.Players][ContainerType.Friend].Remove(thisPtr->GameObject.ObjectID);
						}

						if ((thisPtr->StatusFlags & (byte)StatusFlags.PartyMember) > 0)
						{
							_containers[UnitType.Players][ContainerType.Party].Add(thisPtr->GameObject.ObjectID);
						}
						else
						{
							_containers[UnitType.Players][ContainerType.Party].Remove(thisPtr->GameObject.ObjectID);
						}

						if ((_pluginInterface.ClientState.Condition[ConditionFlag.BoundByDuty]
							|| _pluginInterface.ClientState.Condition[ConditionFlag.BetweenAreas]
							|| _pluginInterface.ClientState.Condition[ConditionFlag.WatchingCutscene])
							&& !_config.TerritoryTypeWhitelist.Contains(_pluginInterface.ClientState.TerritoryType))
						{
							break;
						}

						if (*LocalPlayer->Character.FreeCompanyTag != 0
							&& LocalPlayer->Character.CurrentWorld == LocalPlayer->Character.HomeWorld
							&& UnsafeArrayEqual(thisPtr->FreeCompanyTag, LocalPlayer->Character.FreeCompanyTag, 7))
						{
							_containers[UnitType.Players][ContainerType.Company].Add(thisPtr->GameObject.ObjectID);
						}
						else
						{
							_containers[UnitType.Players][ContainerType.Company].Remove(thisPtr->GameObject.ObjectID);
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
							(_config.ShowDeadPlayer && thisPtr->Health == 0) ||
							(_config.ShowFriendPlayer && _containers[UnitType.Players][ContainerType.Friend].Contains(thisPtr->GameObject.ObjectID)) ||
							(_config.ShowCompanyPlayer && _containers[UnitType.Players][ContainerType.Company].Contains(thisPtr->GameObject.ObjectID)) ||
							(_config.ShowPartyPlayer && _containers[UnitType.Players][ContainerType.Party].Contains(thisPtr->GameObject.ObjectID)) ||
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

						_containers[UnitType.Pets][ContainerType.All].Add(thisPtr->GameObject.ObjectID);

						if (_containers[UnitType.Players][ContainerType.Friend].Contains(thisPtr->GameObject.OwnerID))
						{
							_containers[UnitType.Pets][ContainerType.Friend].Add(thisPtr->GameObject.ObjectID);
						}

						if (_containers[UnitType.Players][ContainerType.Party].Contains(thisPtr->GameObject.OwnerID))
						{
							_containers[UnitType.Pets][ContainerType.Party].Add(thisPtr->GameObject.ObjectID);
						}

						if (_containers[UnitType.Players][ContainerType.Company].Contains(thisPtr->GameObject.OwnerID))
						{
							_containers[UnitType.Pets][ContainerType.Company].Add(thisPtr->GameObject.ObjectID);
						}

						if ((_config.ShowFriendPet && _containers[UnitType.Players][ContainerType.Friend].Contains(thisPtr->GameObject.OwnerID))
							|| (_config.ShowCompanyPet && _containers[UnitType.Players][ContainerType.Company].Contains(thisPtr->GameObject.OwnerID))
							|| (_config.ShowPartyPet && _containers[UnitType.Players][ContainerType.Party].Contains(thisPtr->GameObject.OwnerID)))
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
						    && !_containers[UnitType.Players][ContainerType.Party].Contains(thisPtr->GameObject.OwnerID))
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

						_containers[UnitType.Chocobos][ContainerType.All].Add(thisPtr->GameObject.ObjectID);

						if (_containers[UnitType.Players][ContainerType.Friend].Contains(thisPtr->GameObject.OwnerID))
						{
							_containers[UnitType.Chocobos][ContainerType.Friend].Add(thisPtr->GameObject.ObjectID);
						}

						if (_containers[UnitType.Players][ContainerType.Party].Contains(thisPtr->GameObject.OwnerID))
						{
							_containers[UnitType.Chocobos][ContainerType.Party].Add(thisPtr->GameObject.ObjectID);
						}

						if (_containers[UnitType.Players][ContainerType.Company].Contains(thisPtr->GameObject.OwnerID))
						{
							_containers[UnitType.Chocobos][ContainerType.Company].Add(thisPtr->GameObject.ObjectID);
						}

						if ((_config.ShowFriendChocobo && _containers[UnitType.Players][ContainerType.Friend].Contains(thisPtr->GameObject.OwnerID))
							|| (_config.ShowCompanyChocobo && _containers[UnitType.Players][ContainerType.Company].Contains(thisPtr->GameObject.OwnerID))
							|| (_config.ShowPartyChocobo && _containers[UnitType.Players][ContainerType.Party].Contains(thisPtr->GameObject.OwnerID)))
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
				_containers[UnitType.Players][ContainerType.Party].Add(thisPtr->GameObject.ObjectID);
				thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
				HiddenObjectIds.Remove(thisPtr->GameObject.ObjectID);
			}

			if (_config.HidePlayer
				&& _config.ShowDeadPlayer
				&& thisPtr->GameObject.ObjectKind == (byte)ObjectKind.Player
				&& thisPtr->Health == 0
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
				_containers[UnitType.Minions][ContainerType.All].Add(thisPtr->Character.CompanionOwnerID);

				if (_containers[UnitType.Players][ContainerType.Friend].Contains(thisPtr->Character.CompanionOwnerID))
				{
					_containers[UnitType.Minions][ContainerType.Friend].Add(thisPtr->Character.CompanionOwnerID);
				}

				if (_containers[UnitType.Players][ContainerType.Party].Contains(thisPtr->Character.CompanionOwnerID))
				{
					_containers[UnitType.Minions][ContainerType.Party].Add(thisPtr->Character.CompanionOwnerID);
				}

				if (_containers[UnitType.Players][ContainerType.Company].Contains(thisPtr->Character.CompanionOwnerID))
				{
					_containers[UnitType.Minions][ContainerType.Company].Add(thisPtr->Character.CompanionOwnerID);
				}

				if ((_config.ShowFriendMinion && _containers[UnitType.Players][ContainerType.Friend].Contains(thisPtr->Character.CompanionOwnerID))
					|| (_config.ShowCompanyMinion && _containers[UnitType.Players][ContainerType.Company].Contains(thisPtr->Character.CompanionOwnerID))
					|| (_config.ShowPartyMinion && _containers[UnitType.Players][ContainerType.Party].Contains(thisPtr->Character.CompanionOwnerID)))
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

				foreach (var container in _containers)
				{
					foreach (var set in container.Value)
					{
						set.Value.Remove(thisPtr->GameObject.ObjectID);
					}
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