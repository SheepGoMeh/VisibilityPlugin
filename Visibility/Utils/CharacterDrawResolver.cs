using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Component.GUI;

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
		private readonly HashSet<uint> hiddenObjectIds = new();
		private readonly HashSet<uint> objectIdsToShow = new();

		private readonly Dictionary<UnitType, Dictionary<ContainerType, HashSet<uint>>> containers = new()
		{
			{
				UnitType.Players, new Dictionary<ContainerType, HashSet<uint>>
				{
					{ ContainerType.All, new HashSet<uint>() },
					{ ContainerType.Friend, new HashSet<uint>() },
					{ ContainerType.Party, new HashSet<uint>() },
					{ ContainerType.Company, new HashSet<uint>() },
				}
			},
			{
				UnitType.Pets, new Dictionary<ContainerType, HashSet<uint>>
				{
					{ ContainerType.All, new HashSet<uint>() },
					{ ContainerType.Friend, new HashSet<uint>() },
					{ ContainerType.Party, new HashSet<uint>() },
					{ ContainerType.Company, new HashSet<uint>() },
				}
			},
			{
				UnitType.Chocobos, new Dictionary<ContainerType, HashSet<uint>>
				{
					{ ContainerType.All, new HashSet<uint>() },
					{ ContainerType.Friend, new HashSet<uint>() },
					{ ContainerType.Party, new HashSet<uint>() },
					{ ContainerType.Company, new HashSet<uint>() },
				}
			},
			{
				UnitType.Minions, new Dictionary<ContainerType, HashSet<uint>>
				{
					{ ContainerType.All, new HashSet<uint>() },
					{ ContainerType.Friend, new HashSet<uint>() },
					{ ContainerType.Party, new HashSet<uint>() },
					{ ContainerType.Company, new HashSet<uint>() },
				}
			},
		};

		private readonly HashSet<uint> hiddenMinionObjectIds = new HashSet<uint>();
		private readonly HashSet<uint> minionObjectIdsToShow = new HashSet<uint>();

		private unsafe BattleChara* localPlayer = null;

		// void Client::Game::Character::Character::EnableDraw(Client::Game::Character::Character* thisPtr);
		private unsafe delegate void CharacterEnableDrawPrototype(Character* thisPtr);

		// void Client::Game::Character::Character::DisableDraw(Client::Game::Character::Character* thisPtr);
		private unsafe delegate void CharacterDisableDrawPrototype(Character* thisPtr);

		// void Client::Game::Character::Companion::EnableDraw(Client::Game::Character::Companion* thisPtr);
		private unsafe delegate void CompanionEnableDrawPrototype(Companion* thisPtr);

		// void dtor_Client::Game::Character::Character(Client::Game::Character::Character* thisPtr);
		private unsafe delegate void CharacterDtorPrototype(Character* thisPtr);

		private Hook<CharacterEnableDrawPrototype>? hookCharacterEnableDraw;
		private Hook<CharacterDisableDrawPrototype>? hookCharacterDisableDraw;
		private Hook<CompanionEnableDrawPrototype>? hookCompanionEnableDraw;
		private Hook<CharacterDtorPrototype>? hookCharacterDtor;

		private readonly AddressResolver address = new();

		public unsafe void Init()
		{
			this.address.Setup(VisibilityPlugin.SigScanner);

			var localPlayerAddress = VisibilityPlugin.ClientState.LocalPlayer?.Address;

			if (localPlayerAddress.HasValue && this.localPlayer != (BattleChara*)localPlayerAddress.Value)
			{
				this.localPlayer = (BattleChara*)localPlayerAddress.Value;
			}

			this.hookCharacterEnableDraw = new Hook<CharacterEnableDrawPrototype>(
				this.address.CharacterEnableDrawAddress,
				this.CharacterEnableDrawDetour);
			this.hookCharacterDisableDraw = new Hook<CharacterDisableDrawPrototype>(
				this.address.CharacterDisableDrawAddress,
				this.CharacterDisableDrawDetour);
			this.hookCompanionEnableDraw = new Hook<CompanionEnableDrawPrototype>(
				this.address.CompanionEnableDrawAddress,
				this.CompanionEnableDrawDetour);
			this.hookCharacterDtor = new Hook<CharacterDtorPrototype>(
				this.address.CharacterDtorAddress,
				this.CharacterDtorDetour);

			this.hookCharacterEnableDraw.Enable();
			this.hookCharacterDisableDraw.Enable();
			this.hookCompanionEnableDraw.Enable();
			this.hookCharacterDtor.Enable();
		}

		public void Show(UnitType unitType, ContainerType containerType)
		{
			if (unitType == UnitType.Minions)
			{
				this.minionObjectIdsToShow.UnionWith(this.containers[unitType][containerType]);
				this.hiddenMinionObjectIds.ExceptWith(this.containers[unitType][containerType]);
			}
			else
			{
				this.objectIdsToShow.UnionWith(this.containers[unitType][containerType]);
				this.hiddenObjectIds.ExceptWith(this.containers[unitType][containerType]);
			}
		}

		public void ShowPlayers(ContainerType type) => this.Show(UnitType.Players, type);

		public void ShowPets(ContainerType type) => this.Show(UnitType.Pets, type);

		public void ShowChocobos(ContainerType type) => this.Show(UnitType.Chocobos, type);

		public void ShowMinions(ContainerType type) => this.Show(UnitType.Minions, type);

		public unsafe void ShowAll()
		{
			foreach (var actor in VisibilityPlugin.ObjectTable)
			{
				var thisPtr = (Character*)actor.Address;

				if (thisPtr->GameObject.ObjectKind == (byte)ObjectKind.Companion)
				{
					if (!this.hiddenMinionObjectIds.Contains(thisPtr->CompanionOwnerID))
					{
						continue;
					}

					thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
					this.minionObjectIdsToShow.Remove(thisPtr->CompanionOwnerID);
				}
				else
				{
					if (!this.hiddenObjectIds.Contains(thisPtr->GameObject.ObjectID))
					{
						continue;
					}

					thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
					this.hiddenObjectIds.Remove(thisPtr->GameObject.ObjectID);
				}
			}
		}

		public void ShowPlayer(uint id)
		{
			if (!this.hiddenObjectIds.Contains(id))
			{
				return;
			}

			this.objectIdsToShow.Add(id);
			this.hiddenObjectIds.Remove(id);
		}

		private static unsafe bool UnsafeArrayEqual(byte* arr1, byte* arr2, int len)
		{
			var a1 = new ReadOnlySpan<byte>(arr1, len);
			var a2 = new ReadOnlySpan<byte>(arr2, len);
			return a1.SequenceEqual(a2);
		}

		private static unsafe bool UnsafeArrayEqual(byte[] arr1, byte* arr2, int len)
		{
			fixed (byte* a1 = arr1)
			{
				return UnsafeArrayEqual(a1, arr2, len);
			}
		}

		private unsafe void CharacterEnableDrawDetour(Character* thisPtr)
		{
			var localPlayerAddress = VisibilityPlugin.ClientState.LocalPlayer?.Address;

			if (localPlayerAddress.HasValue && this.localPlayer != (BattleChara*)localPlayerAddress.Value)
			{
				this.localPlayer = (BattleChara*)localPlayerAddress.Value;
			}

			var nowLoadingWidget = VisibilityPlugin.GameGui.GetAddonByName("NowLoading", 1);

			if (VisibilityPlugin.Instance.Configuration.Enabled && nowLoadingWidget != IntPtr.Zero &&
			    !((AtkUnitBase*)nowLoadingWidget)->IsVisible && localPlayerAddress.HasValue &&
			    thisPtr != (Character*)this.localPlayer)
			{
				switch (thisPtr->GameObject.ObjectKind)
				{
					case (byte)ObjectKind.Player:
						if (thisPtr->GameObject.ObjectID == 0xE0000000)
						{
							break;
						}

						this.containers[UnitType.Players][ContainerType.All].Add(thisPtr->GameObject.ObjectID);

						if ((thisPtr->StatusFlags & (byte)StatusFlags.Friend) > 0)
						{
							this.containers[UnitType.Players][ContainerType.Friend].Add(thisPtr->GameObject.ObjectID);
						}
						else
						{
							this.containers[UnitType.Players][ContainerType.Friend]
								.Remove(thisPtr->GameObject.ObjectID);
						}

						if ((thisPtr->StatusFlags & (byte)StatusFlags.PartyMember) > 0)
						{
							this.containers[UnitType.Players][ContainerType.Party].Add(thisPtr->GameObject.ObjectID);
						}
						else
						{
							this.containers[UnitType.Players][ContainerType.Party].Remove(thisPtr->GameObject.ObjectID);
						}

						if ((VisibilityPlugin.Condition[ConditionFlag.BoundByDuty]
						     || VisibilityPlugin.Condition[ConditionFlag.BetweenAreas]
						     || VisibilityPlugin.Condition[ConditionFlag.WatchingCutscene])
						    && !VisibilityPlugin.Instance.Configuration.TerritoryTypeWhitelist.Contains(
							    VisibilityPlugin.ClientState.TerritoryType))
						{
							break;
						}

						if (*this.localPlayer->Character.FreeCompanyTag != 0
						    && this.localPlayer->Character.CurrentWorld == this.localPlayer->Character.HomeWorld
						    && UnsafeArrayEqual(thisPtr->FreeCompanyTag, this.localPlayer->Character.FreeCompanyTag, 7))
						{
							this.containers[UnitType.Players][ContainerType.Company].Add(thisPtr->GameObject.ObjectID);
						}
						else
						{
							this.containers[UnitType.Players][ContainerType.Company]
								.Remove(thisPtr->GameObject.ObjectID);
						}

						if (VisibilityPlugin.Instance.Configuration.VoidList.Any(
							    x => UnsafeArrayEqual(
								         x.NameBytes,
								         thisPtr->GameObject.Name,
								         x.NameBytes.Length) &&
							         x.HomeworldId == thisPtr->HomeWorld))
						{
							thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
							this.hiddenObjectIds.Add(thisPtr->GameObject.ObjectID);
							break;
						}

						if (!VisibilityPlugin.Instance.Configuration.HidePlayer ||
						    (VisibilityPlugin.Instance.Configuration.ShowDeadPlayer && thisPtr->Health == 0) ||
						    (VisibilityPlugin.Instance.Configuration.ShowFriendPlayer &&
						     this.containers[UnitType.Players][ContainerType.Friend]
							     .Contains(thisPtr->GameObject.ObjectID)) ||
						    (VisibilityPlugin.Instance.Configuration.ShowCompanyPlayer &&
						     this.containers[UnitType.Players][ContainerType.Company]
							     .Contains(thisPtr->GameObject.ObjectID)) ||
						    (VisibilityPlugin.Instance.Configuration.ShowPartyPlayer &&
						     this.containers[UnitType.Players][ContainerType.Party]
							     .Contains(thisPtr->GameObject.ObjectID)) ||
						    VisibilityPlugin.Instance.Configuration.Whitelist.Any(
							    x => UnsafeArrayEqual(
								         x.NameBytes,
								         thisPtr->GameObject.Name,
								         x.NameBytes.Length) &&
							         x.HomeworldId == thisPtr->HomeWorld))
						{
							break;
						}
						else
						{
							thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
							this.hiddenObjectIds.Add(thisPtr->GameObject.ObjectID);
							break;
						}
					case (byte)ObjectKind.BattleNpc when thisPtr->GameObject.SubKind == (byte)BattleNpcSubKind.Pet &&
					                                     thisPtr->NameID != 6565:
						if (!VisibilityPlugin.Instance.Configuration.HidePet
						    || thisPtr->GameObject.OwnerID == this.localPlayer->Character.GameObject.ObjectID)
						{
							break;
						}

						this.containers[UnitType.Pets][ContainerType.All].Add(thisPtr->GameObject.ObjectID);

						if (this.containers[UnitType.Players][ContainerType.Friend]
						    .Contains(thisPtr->GameObject.OwnerID))
						{
							this.containers[UnitType.Pets][ContainerType.Friend].Add(thisPtr->GameObject.ObjectID);
						}

						if (this.containers[UnitType.Players][ContainerType.Party]
						    .Contains(thisPtr->GameObject.OwnerID))
						{
							this.containers[UnitType.Pets][ContainerType.Party].Add(thisPtr->GameObject.ObjectID);
						}

						if (this.containers[UnitType.Players][ContainerType.Company]
						    .Contains(thisPtr->GameObject.OwnerID))
						{
							this.containers[UnitType.Pets][ContainerType.Company].Add(thisPtr->GameObject.ObjectID);
						}

						if ((VisibilityPlugin.Instance.Configuration.ShowFriendPet &&
						     this.containers[UnitType.Players][ContainerType.Friend]
							     .Contains(thisPtr->GameObject.OwnerID))
						    || (VisibilityPlugin.Instance.Configuration.ShowCompanyPet &&
						        this.containers[UnitType.Players][ContainerType.Company]
							        .Contains(thisPtr->GameObject.OwnerID))
						    || (VisibilityPlugin.Instance.Configuration.ShowPartyPet &&
						        this.containers[UnitType.Players][ContainerType.Party]
							        .Contains(thisPtr->GameObject.OwnerID)))
						{
							break;
						}
						else
						{
							thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
							this.hiddenObjectIds.Add(thisPtr->GameObject.ObjectID);
							break;
						}
					case (byte)ObjectKind.BattleNpc
						when thisPtr->GameObject.SubKind == (byte)BattleNpcSubKind.Pet && thisPtr->NameID == 6565
						: // Earthly Star
						if (VisibilityPlugin.Instance.Configuration.HideStar
						    && VisibilityPlugin.Condition[ConditionFlag.InCombat]
						    && thisPtr->GameObject.OwnerID != this.localPlayer->Character.GameObject.ObjectID
						    && !this.containers[UnitType.Players][ContainerType.Party]
							    .Contains(thisPtr->GameObject.OwnerID))
						{
							thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
							this.hiddenObjectIds.Add(thisPtr->GameObject.ObjectID);
						}

						break;
					case (byte)ObjectKind.BattleNpc when thisPtr->GameObject.SubKind == (byte)BattleNpcSubKind.Chocobo:
						if (!VisibilityPlugin.Instance.Configuration.HideChocobo
						    || thisPtr->GameObject.OwnerID == this.localPlayer->Character.GameObject.ObjectID)
						{
							break;
						}

						this.containers[UnitType.Chocobos][ContainerType.All].Add(thisPtr->GameObject.ObjectID);

						if (this.containers[UnitType.Players][ContainerType.Friend]
						    .Contains(thisPtr->GameObject.OwnerID))
						{
							this.containers[UnitType.Chocobos][ContainerType.Friend].Add(thisPtr->GameObject.ObjectID);
						}

						if (this.containers[UnitType.Players][ContainerType.Party]
						    .Contains(thisPtr->GameObject.OwnerID))
						{
							this.containers[UnitType.Chocobos][ContainerType.Party].Add(thisPtr->GameObject.ObjectID);
						}

						if (this.containers[UnitType.Players][ContainerType.Company]
						    .Contains(thisPtr->GameObject.OwnerID))
						{
							this.containers[UnitType.Chocobos][ContainerType.Company].Add(thisPtr->GameObject.ObjectID);
						}

						if ((VisibilityPlugin.Instance.Configuration.ShowFriendChocobo &&
						     this.containers[UnitType.Players][ContainerType.Friend]
							     .Contains(thisPtr->GameObject.OwnerID))
						    || (VisibilityPlugin.Instance.Configuration.ShowCompanyChocobo &&
						        this.containers[UnitType.Players][ContainerType.Company]
							        .Contains(thisPtr->GameObject.OwnerID))
						    || (VisibilityPlugin.Instance.Configuration.ShowPartyChocobo &&
						        this.containers[UnitType.Players][ContainerType.Party]
							        .Contains(thisPtr->GameObject.OwnerID)))
						{
							break;
						}
						else
						{
							thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
							this.hiddenObjectIds.Add(thisPtr->GameObject.ObjectID);
							break;
						}
				}
			}

			this.hookCharacterEnableDraw!.Original(thisPtr);
		}

		private unsafe void CharacterDisableDrawDetour(Character* thisPtr)
		{
			var nowLoadingWidget = VisibilityPlugin.GameGui.GetAddonByName("NowLoading", 1);

			if (nowLoadingWidget != IntPtr.Zero && !((AtkUnitBase*)nowLoadingWidget)->IsVisible &&
			    !VisibilityPlugin.Condition[ConditionFlag.WatchingCutscene])
			{
				if (thisPtr->GameObject.ObjectKind == (byte)ObjectKind.Player
				    && (thisPtr->StatusFlags & (byte)StatusFlags.PartyMember) > 0)
				{
					this.containers[UnitType.Players][ContainerType.Party].Add(thisPtr->GameObject.ObjectID);
					thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
					this.hiddenObjectIds.Remove(thisPtr->GameObject.ObjectID);
				}

				if (VisibilityPlugin.Instance.Configuration.HidePlayer
				    && VisibilityPlugin.Instance.Configuration.ShowDeadPlayer
				    && thisPtr->GameObject.ObjectKind == (byte)ObjectKind.Player
				    && thisPtr->Health == 0
				    && this.hiddenObjectIds.Contains(thisPtr->GameObject.ObjectID))
				{
					thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
					this.hiddenObjectIds.Remove(thisPtr->GameObject.ObjectID);
				}
				else if (this.objectIdsToShow.Contains(thisPtr->GameObject.ObjectID))
				{
					thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
					this.objectIdsToShow.Remove(thisPtr->GameObject.ObjectID);
				}
				else if (this.minionObjectIdsToShow.Contains(thisPtr->CompanionOwnerID))
				{
					thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
					this.minionObjectIdsToShow.Remove(thisPtr->CompanionOwnerID);
				}
			}

			this.hookCharacterDisableDraw!.Original(thisPtr);
		}

		private unsafe void CompanionEnableDrawDetour(Companion* thisPtr)
		{
			if (VisibilityPlugin.Instance.Configuration.Enabled
			    && VisibilityPlugin.Instance.Configuration.HideMinion
			    && this.localPlayer != null
			    && thisPtr->Character.CompanionOwnerID != this.localPlayer->Character.GameObject.ObjectID)
			{
				this.containers[UnitType.Minions][ContainerType.All].Add(thisPtr->Character.CompanionOwnerID);

				if (this.containers[UnitType.Players][ContainerType.Friend]
				    .Contains(thisPtr->Character.CompanionOwnerID))
				{
					this.containers[UnitType.Minions][ContainerType.Friend].Add(thisPtr->Character.CompanionOwnerID);
				}

				if (this.containers[UnitType.Players][ContainerType.Party]
				    .Contains(thisPtr->Character.CompanionOwnerID))
				{
					this.containers[UnitType.Minions][ContainerType.Party].Add(thisPtr->Character.CompanionOwnerID);
				}

				if (this.containers[UnitType.Players][ContainerType.Company]
				    .Contains(thisPtr->Character.CompanionOwnerID))
				{
					this.containers[UnitType.Minions][ContainerType.Company].Add(thisPtr->Character.CompanionOwnerID);
				}

				if ((VisibilityPlugin.Instance.Configuration.ShowFriendMinion &&
				     this.containers[UnitType.Players][ContainerType.Friend]
					     .Contains(thisPtr->Character.CompanionOwnerID))
				    || (VisibilityPlugin.Instance.Configuration.ShowCompanyMinion &&
				        this.containers[UnitType.Players][ContainerType.Company]
					        .Contains(thisPtr->Character.CompanionOwnerID))
				    || (VisibilityPlugin.Instance.Configuration.ShowPartyMinion &&
				        this.containers[UnitType.Players][ContainerType.Party]
					        .Contains(thisPtr->Character.CompanionOwnerID)))
				{
				}
				else
				{
					thisPtr->Character.GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
					this.hiddenMinionObjectIds.Add(thisPtr->Character.CompanionOwnerID);
				}
			}

			this.hookCompanionEnableDraw!.Original(thisPtr);
		}

		private unsafe void CharacterDtorDetour(Character* thisPtr)
		{
			if (thisPtr->GameObject.ObjectKind == (byte)ObjectKind.Player
			    || thisPtr->GameObject.ObjectKind == (byte)ObjectKind.Companion
			    || (thisPtr->GameObject.ObjectKind == (byte)ObjectKind.BattleNpc
			        && (thisPtr->GameObject.SubKind == (byte)BattleNpcSubKind.Pet ||
			            thisPtr->GameObject.SubKind == 3)))
			{
				this.hiddenObjectIds.Remove(thisPtr->GameObject.ObjectID);
				this.hiddenMinionObjectIds.Remove(thisPtr->GameObject.ObjectID);

				foreach (var container in this.containers)
				{
					foreach (var set in container.Value)
					{
						set.Value.Remove(thisPtr->GameObject.ObjectID);
					}
				}
			}

			this.hookCharacterDtor!.Original(thisPtr);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			this.hookCharacterEnableDraw?.Dispose();
			this.hookCharacterDisableDraw?.Dispose();
			this.hookCompanionEnableDraw?.Dispose();
			this.hookCharacterDtor?.Dispose();
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			this.Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}