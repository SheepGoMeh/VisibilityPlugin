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
		private HashSet<uint> HiddenObjectIds = new();
		private HashSet<uint> ObjectIdsToShow = new();

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

		private HashSet<uint> HiddenMinionObjectIds = new HashSet<uint>();
		private HashSet<uint> MinionObjectIdsToShow = new HashSet<uint>();

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
		private VisibilityPlugin? plugin;

		public unsafe void Init(VisibilityPlugin visibilityPlugin)
		{
			this.plugin = visibilityPlugin;
			this.address.Setup(this.plugin.SigScanner);

			var localPlayerAddress = this.plugin!.ClientState.LocalPlayer?.Address;

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
				this.MinionObjectIdsToShow.UnionWith(this.containers[unitType][containerType]);
				this.HiddenMinionObjectIds.ExceptWith(this.containers[unitType][containerType]);
			}
			else
			{
				this.ObjectIdsToShow.UnionWith(this.containers[unitType][containerType]);
				this.HiddenObjectIds.ExceptWith(this.containers[unitType][containerType]);
			}
		}

		public void ShowPlayers(ContainerType type) => this.Show(UnitType.Players, type);

		public void ShowPets(ContainerType type) => this.Show(UnitType.Pets, type);

		public void ShowChocobos(ContainerType type) => this.Show(UnitType.Chocobos, type);

		public void ShowMinions(ContainerType type) => this.Show(UnitType.Minions, type);

		public unsafe void ShowAll()
		{
			foreach (var actor in this.plugin!.ObjectTable)
			{
				var thisPtr = (Character*)actor.Address;

				if (thisPtr->GameObject.ObjectKind == (byte)ObjectKind.Companion)
				{
					if (!this.HiddenMinionObjectIds.Contains(thisPtr->CompanionOwnerID))
					{
						continue;
					}

					thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
					this.MinionObjectIdsToShow.Remove(thisPtr->CompanionOwnerID);
				}
				else
				{
					if (!this.HiddenObjectIds.Contains(thisPtr->GameObject.ObjectID))
					{
						continue;
					}

					thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
					this.HiddenObjectIds.Remove(thisPtr->GameObject.ObjectID);
				}
			}
		}

		public void ShowPlayer(uint id)
		{
			if (!this.HiddenObjectIds.Contains(id))
			{
				return;
			}

			this.ObjectIdsToShow.Add(id);
			this.HiddenObjectIds.Remove(id);
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
			var localPlayerAddress = this.plugin!.ClientState.LocalPlayer?.Address;

			if (localPlayerAddress.HasValue && this.localPlayer != (BattleChara*)localPlayerAddress.Value)
			{
				this.localPlayer = (BattleChara*)localPlayerAddress.Value;
			}

			var nowLoadingWidget = this.plugin.GameGui.GetAddonByName("NowLoading", 1);

			if (this.plugin.Configuration.Enabled && nowLoadingWidget != IntPtr.Zero &&
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

						if ((this.plugin.Condition[ConditionFlag.BoundByDuty]
						     || this.plugin.Condition[ConditionFlag.BetweenAreas]
						     || this.plugin.Condition[ConditionFlag.WatchingCutscene])
						    && !this.plugin.Configuration.TerritoryTypeWhitelist.Contains(
							    this.plugin.ClientState.TerritoryType))
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

						if (this.plugin.Configuration.VoidList.Any(
							    x => UnsafeArrayEqual(
								         x.NameBytes,
								         thisPtr->GameObject.Name,
								         x.NameBytes.Length) &&
							         x.HomeworldId == thisPtr->HomeWorld))
						{
							thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
							this.HiddenObjectIds.Add(thisPtr->GameObject.ObjectID);
							break;
						}

						if (!this.plugin.Configuration.HidePlayer ||
						    (this.plugin.Configuration.ShowDeadPlayer && thisPtr->Health == 0) ||
						    (this.plugin.Configuration.ShowFriendPlayer &&
						     this.containers[UnitType.Players][ContainerType.Friend]
							     .Contains(thisPtr->GameObject.ObjectID)) ||
						    (this.plugin.Configuration.ShowCompanyPlayer &&
						     this.containers[UnitType.Players][ContainerType.Company]
							     .Contains(thisPtr->GameObject.ObjectID)) ||
						    (this.plugin.Configuration.ShowPartyPlayer &&
						     this.containers[UnitType.Players][ContainerType.Party]
							     .Contains(thisPtr->GameObject.ObjectID)) ||
						    this.plugin.Configuration.Whitelist.Any(
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
							this.HiddenObjectIds.Add(thisPtr->GameObject.ObjectID);
							break;
						}
					case (byte)ObjectKind.BattleNpc when thisPtr->GameObject.SubKind == (byte)BattleNpcSubKind.Pet &&
					                                     thisPtr->NameID != 6565:
						if (!this.plugin.Configuration.HidePet
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

						if ((this.plugin.Configuration.ShowFriendPet &&
						     this.containers[UnitType.Players][ContainerType.Friend]
							     .Contains(thisPtr->GameObject.OwnerID))
						    || (this.plugin.Configuration.ShowCompanyPet &&
						        this.containers[UnitType.Players][ContainerType.Company]
							        .Contains(thisPtr->GameObject.OwnerID))
						    || (this.plugin.Configuration.ShowPartyPet &&
						        this.containers[UnitType.Players][ContainerType.Party]
							        .Contains(thisPtr->GameObject.OwnerID)))
						{
							break;
						}
						else
						{
							thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
							this.HiddenObjectIds.Add(thisPtr->GameObject.ObjectID);
							break;
						}
					case (byte)ObjectKind.BattleNpc
						when thisPtr->GameObject.SubKind == (byte)BattleNpcSubKind.Pet && thisPtr->NameID == 6565
						: // Earthly Star
						if (this.plugin.Configuration.HideStar
						    && this.plugin.Condition[ConditionFlag.InCombat]
						    && thisPtr->GameObject.OwnerID != this.localPlayer->Character.GameObject.ObjectID
						    && !this.containers[UnitType.Players][ContainerType.Party]
							    .Contains(thisPtr->GameObject.OwnerID))
						{
							thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
							this.HiddenObjectIds.Add(thisPtr->GameObject.ObjectID);
						}

						break;
					case (byte)ObjectKind.BattleNpc when thisPtr->GameObject.SubKind == (byte)BattleNpcSubKind.Chocobo:
						if (!this.plugin.Configuration.HideChocobo
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

						if ((this.plugin.Configuration.ShowFriendChocobo &&
						     this.containers[UnitType.Players][ContainerType.Friend]
							     .Contains(thisPtr->GameObject.OwnerID))
						    || (this.plugin.Configuration.ShowCompanyChocobo &&
						        this.containers[UnitType.Players][ContainerType.Company]
							        .Contains(thisPtr->GameObject.OwnerID))
						    || (this.plugin.Configuration.ShowPartyChocobo &&
						        this.containers[UnitType.Players][ContainerType.Party]
							        .Contains(thisPtr->GameObject.OwnerID)))
						{
							break;
						}
						else
						{
							thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
							this.HiddenObjectIds.Add(thisPtr->GameObject.ObjectID);
							break;
						}
				}
			}

			this.hookCharacterEnableDraw!.Original(thisPtr);
		}

		private unsafe void CharacterDisableDrawDetour(Character* thisPtr)
		{
			var nowLoadingWidget = this.plugin!.GameGui.GetAddonByName("NowLoading", 1);

			if (nowLoadingWidget != IntPtr.Zero && !((AtkUnitBase*)nowLoadingWidget)->IsVisible &&
			    !this.plugin.Condition[ConditionFlag.WatchingCutscene])
			{
				if (thisPtr->GameObject.ObjectKind == (byte)ObjectKind.Player
				    && (thisPtr->StatusFlags & (byte)StatusFlags.PartyMember) > 0)
				{
					this.containers[UnitType.Players][ContainerType.Party].Add(thisPtr->GameObject.ObjectID);
					thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
					this.HiddenObjectIds.Remove(thisPtr->GameObject.ObjectID);
				}

				if (this.plugin.Configuration.HidePlayer
				    && this.plugin.Configuration.ShowDeadPlayer
				    && thisPtr->GameObject.ObjectKind == (byte)ObjectKind.Player
				    && thisPtr->Health == 0
				    && this.HiddenObjectIds.Contains(thisPtr->GameObject.ObjectID))
				{
					thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
					this.HiddenObjectIds.Remove(thisPtr->GameObject.ObjectID);
				}
				else if (this.ObjectIdsToShow.Contains(thisPtr->GameObject.ObjectID))
				{
					thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
					this.ObjectIdsToShow.Remove(thisPtr->GameObject.ObjectID);
				}
				else if (this.MinionObjectIdsToShow.Contains(thisPtr->CompanionOwnerID))
				{
					thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
					this.MinionObjectIdsToShow.Remove(thisPtr->CompanionOwnerID);
				}
			}

			this.hookCharacterDisableDraw!.Original(thisPtr);
		}

		private unsafe void CompanionEnableDrawDetour(Companion* thisPtr)
		{
			if (this.plugin!.Configuration.Enabled
			    && this.plugin.Configuration.HideMinion
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

				if ((this.plugin.Configuration.ShowFriendMinion &&
				     this.containers[UnitType.Players][ContainerType.Friend]
					     .Contains(thisPtr->Character.CompanionOwnerID))
				    || (this.plugin.Configuration.ShowCompanyMinion &&
				        this.containers[UnitType.Players][ContainerType.Company]
					        .Contains(thisPtr->Character.CompanionOwnerID))
				    || (this.plugin.Configuration.ShowPartyMinion &&
				        this.containers[UnitType.Players][ContainerType.Party]
					        .Contains(thisPtr->Character.CompanionOwnerID)))
				{
				}
				else
				{
					thisPtr->Character.GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
					this.HiddenMinionObjectIds.Add(thisPtr->Character.CompanionOwnerID);
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
				this.HiddenObjectIds.Remove(thisPtr->GameObject.ObjectID);
				this.HiddenMinionObjectIds.Remove(thisPtr->GameObject.ObjectID);

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