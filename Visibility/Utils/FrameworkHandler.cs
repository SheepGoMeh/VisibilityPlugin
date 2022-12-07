using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Visibility.Utils;

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

public class FrameworkHandler : IDisposable
{
	private enum ObjectType
	{
		Character,
		Companion
	}

	private readonly HashSet<uint> hiddenObjectIds = new ();
	private readonly HashSet<uint> objectIdsToShow = new ();
	private readonly HashSet<uint> checkedVoidedObjectIds = new (capacity: 1000);
	private readonly HashSet<uint> checkedWhitelistedObjectIds = new (capacity: 1000);
	private readonly HashSet<uint> voidedObjectIds = new (capacity: 1000);
	private readonly HashSet<uint> whitelistedObjectIds = new (capacity: 1000);

	private readonly Dictionary<UnitType, Dictionary<ContainerType, HashSet<uint>>> containers = new ()
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

	private readonly HashSet<uint> hiddenMinionObjectIds = new ();
	private readonly HashSet<uint> minionObjectIdsToShow = new ();
	private bool isChangingTerritory;

	public unsafe void Update()
	{
		var localPlayerGameObject =
			FFXIVClientStructs.FFXIV.Client.Game.Object.GameObjectManager.GetGameObjectByIndex(0);
		var namePlateWidget = VisibilityPlugin.GameGui.GetAddonByName("NamePlate", 1);

		if (namePlateWidget == IntPtr.Zero || !((AtkUnitBase*)namePlateWidget)->IsVisible ||
		    localPlayerGameObject == null || localPlayerGameObject->ObjectID == 0xE0000000 ||
		    VisibilityPlugin.Instance.Disable || this.isChangingTerritory)
		{
			return;
		}

		var isBound = VisibilityPlugin.Condition[ConditionFlag.BoundByDuty]
		              || VisibilityPlugin.Condition[ConditionFlag.BetweenAreas]
		              || VisibilityPlugin.Condition[ConditionFlag.WatchingCutscene]
		              || VisibilityPlugin.Condition[ConditionFlag.DutyRecorderPlayback];

		var localPlayer = (Character*)localPlayerGameObject;

		for (var i = 1; i != 200; ++i)
		{
			var gameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObjectManager.GetGameObjectByIndex(i);
			var characterPtr = (Character*)gameObject;

			if (gameObject == null || gameObject == localPlayerGameObject || !gameObject->IsCharacter())
			{
				continue;
			}

			switch ((ObjectKind)characterPtr->GameObject.ObjectKind)
			{
				case ObjectKind.Player:
					this.PlayerHandler(characterPtr, localPlayer, isBound);
					break;
				case ObjectKind.BattleNpc when characterPtr->GameObject.SubKind == (byte)BattleNpcSubKind.Pet &&
				                               characterPtr->NameID != 6565:
					this.PetHandler(characterPtr, localPlayer, isBound);
					break;
				case ObjectKind.BattleNpc
					when characterPtr->GameObject.SubKind == (byte)BattleNpcSubKind.Pet && characterPtr->NameID == 6565
					: // Earthly Star
				{
					if (VisibilityPlugin.Instance.Configuration.Enabled &&
					    VisibilityPlugin.Instance.Configuration.HideStar
					    && VisibilityPlugin.Condition[ConditionFlag.InCombat]
					    && characterPtr->GameObject.OwnerID != localPlayer->GameObject.ObjectID
					    && !this.containers[UnitType.Players][ContainerType.Party]
						    .Contains(characterPtr->GameObject.OwnerID))
					{
						this.HideGameObject(characterPtr);
					}

					break;
				}
				case ObjectKind.BattleNpc when characterPtr->GameObject.SubKind == (byte)BattleNpcSubKind.Chocobo:
					this.ChocoboHandler(characterPtr, localPlayer);
					break;
			}
		}

		this.proximity = false;
	}

	private unsafe void PlayerHandler(Character* characterPtr, Character* localPlayer, bool isBound)
	{
		if (characterPtr->GameObject.ObjectID == 0xE0000000 ||
		    this.ShowGameObject(characterPtr))
		{
			return;
		}

		this.containers[UnitType.Players][ContainerType.All].Add(characterPtr->GameObject.ObjectID);

		var isFriend = characterPtr->StatusFlags.TestFlag(StatusFlags.Friend);

		if (isFriend)
		{
			this.containers[UnitType.Players][ContainerType.Friend].Add(characterPtr->GameObject.ObjectID);
		}
		else
		{
			this.containers[UnitType.Players][ContainerType.Friend]
				.Remove(characterPtr->GameObject.ObjectID);
		}

		var isObjectIdInParty = IsObjectIdInParty(characterPtr->GameObject.ObjectID);

		if (isObjectIdInParty)
		{
			this.containers[UnitType.Players][ContainerType.Party].Add(characterPtr->GameObject.ObjectID);
		}
		else
		{
			this.containers[UnitType.Players][ContainerType.Party].Remove(characterPtr->GameObject.ObjectID);
		}

		if (isBound && !VisibilityPlugin.Instance.Configuration.TerritoryTypeWhitelist.Contains(
			    VisibilityPlugin.ClientState.TerritoryType))
		{
			return;
		}

		if (*localPlayer->FreeCompanyTag != 0
		    && localPlayer->CurrentWorld == localPlayer->HomeWorld
		    && UnsafeArrayEqual(characterPtr->FreeCompanyTag, localPlayer->FreeCompanyTag, 7))
		{
			this.containers[UnitType.Players][ContainerType.Company].Add(characterPtr->GameObject.ObjectID);
		}
		else
		{
			this.containers[UnitType.Players][ContainerType.Company]
				.Remove(characterPtr->GameObject.ObjectID);
		}

		if (!this.checkedVoidedObjectIds.Contains(characterPtr->GameObject.ObjectID))
		{
			var voidedPlayer = VisibilityPlugin.Instance.Configuration.VoidList.Find(
				x => UnsafeArrayEqual(x.NameBytes, characterPtr->GameObject.Name, x.NameBytes.Length) &&
				     x.HomeworldId == characterPtr->HomeWorld);

			if (voidedPlayer != null)
			{
				voidedPlayer.ObjectId = characterPtr->GameObject.ObjectID;
				this.voidedObjectIds.Add(characterPtr->GameObject.ObjectID);
			}

			this.checkedVoidedObjectIds.Add(characterPtr->GameObject.ObjectID);
		}

		if (this.voidedObjectIds.Contains(characterPtr->GameObject.ObjectID))
		{
			this.HideGameObject(characterPtr);
			return;
		}

		if (((VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowDeadPlayer &&
		      characterPtr->GameObject.IsDead()) ||
		     (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowPartyPlayer && isObjectIdInParty) ||
		     (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowFriendPlayer && isFriend)) &&
		    this.hiddenObjectIds.Contains(characterPtr->GameObject.ObjectID))
		{
			characterPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
			this.hiddenObjectIds.Remove(characterPtr->GameObject.ObjectID);
			return;
		}

		if (!VisibilityPlugin.Instance.Configuration.Enabled ||
		    !VisibilityPlugin.Instance.Configuration.CurrentConfig.HidePlayer ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowDeadPlayer &&
		     characterPtr->GameObject.IsDead()) ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowFriendPlayer &&
		     this.containers[UnitType.Players][ContainerType.Friend]
			     .Contains(characterPtr->GameObject.ObjectID)) ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowCompanyPlayer &&
		     this.containers[UnitType.Players][ContainerType.Company]
			     .Contains(characterPtr->GameObject.ObjectID)) ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowPartyPlayer &&
		     this.containers[UnitType.Players][ContainerType.Party]
			     .Contains(characterPtr->GameObject.ObjectID)))
		{
			this.MinionHandler(characterPtr->CompanionObject, localPlayer);
			return;
		}

		if (!this.checkedWhitelistedObjectIds.Contains(characterPtr->GameObject.ObjectID))
		{
			var whitelistedPlayer = VisibilityPlugin.Instance.Configuration.Whitelist.Find(
				x => UnsafeArrayEqual(x.NameBytes, characterPtr->GameObject.Name, x.NameBytes.Length) &&
				     x.HomeworldId == characterPtr->HomeWorld);

			if (whitelistedPlayer != null)
			{
				whitelistedPlayer.ObjectId = characterPtr->GameObject.ObjectID;
				this.whitelistedObjectIds.Add(characterPtr->GameObject.ObjectID);
			}

			this.checkedWhitelistedObjectIds.Add(characterPtr->GameObject.ObjectID);
		}

		if (this.whitelistedObjectIds.Contains(characterPtr->GameObject.ObjectID))
		{
			this.MinionHandler(characterPtr->CompanionObject, localPlayer);
			return;
		}

		this.HideGameObject(characterPtr);
	}

	private unsafe void PetHandler(Character* characterPtr, Character* localPlayer, bool isBound)
	{
		// Ignore own pet
		if (characterPtr->GameObject.OwnerID == localPlayer->GameObject.ObjectID ||
		    this.ShowGameObject(characterPtr))
		{
			return;
		}

		this.containers[UnitType.Pets][ContainerType.All].Add(characterPtr->GameObject.ObjectID);

		if (this.containers[UnitType.Players][ContainerType.Friend]
		    .Contains(characterPtr->GameObject.OwnerID))
		{
			this.containers[UnitType.Pets][ContainerType.Friend].Add(characterPtr->GameObject.ObjectID);
		}

		if (this.containers[UnitType.Players][ContainerType.Party]
		    .Contains(characterPtr->GameObject.OwnerID))
		{
			this.containers[UnitType.Pets][ContainerType.Party].Add(characterPtr->GameObject.ObjectID);
		}

		if (this.containers[UnitType.Players][ContainerType.Company]
		    .Contains(characterPtr->GameObject.OwnerID))
		{
			this.containers[UnitType.Pets][ContainerType.Company].Add(characterPtr->GameObject.ObjectID);
		}

		// Do not hide pets in duties
		if (isBound)
		{
			return;
		}

		// Hide pet if it belongs to a voided player
		if (this.voidedObjectIds.Contains(characterPtr->GameObject.OwnerID))
		{
			this.HideGameObject(characterPtr);
			return;
		}

		if (!VisibilityPlugin.Instance.Configuration.Enabled ||
		    !VisibilityPlugin.Instance.Configuration.CurrentConfig.HidePet ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowFriendPet &&
		     this.containers[UnitType.Players][ContainerType.Friend]
			     .Contains(characterPtr->GameObject.OwnerID)) ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowCompanyPet &&
		     this.containers[UnitType.Players][ContainerType.Company]
			     .Contains(characterPtr->GameObject.OwnerID)) ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowPartyPet &&
		     this.containers[UnitType.Players][ContainerType.Party]
			     .Contains(characterPtr->GameObject.OwnerID)) ||
		    this.whitelistedObjectIds.Contains(characterPtr->GameObject.OwnerID))
		{
			return;
		}

		this.HideGameObject(characterPtr);
	}

	private unsafe void ChocoboHandler(Character* characterPtr, Character* localPlayer)
	{
		// Ignore own chocobo
		if (characterPtr->GameObject.OwnerID == localPlayer->GameObject.ObjectID ||
		    this.ShowGameObject(characterPtr))
		{
			return;
		}

		this.containers[UnitType.Chocobos][ContainerType.All].Add(characterPtr->GameObject.ObjectID);

		if (this.containers[UnitType.Players][ContainerType.Friend]
		    .Contains(characterPtr->GameObject.OwnerID))
		{
			this.containers[UnitType.Chocobos][ContainerType.Friend].Add(characterPtr->GameObject.ObjectID);
		}

		if (this.containers[UnitType.Players][ContainerType.Party]
		    .Contains(characterPtr->GameObject.OwnerID))
		{
			this.containers[UnitType.Chocobos][ContainerType.Party].Add(characterPtr->GameObject.ObjectID);
		}

		if (this.containers[UnitType.Players][ContainerType.Company]
		    .Contains(characterPtr->GameObject.OwnerID))
		{
			this.containers[UnitType.Chocobos][ContainerType.Company].Add(characterPtr->GameObject.ObjectID);
		}

		// Hide chocobo if it belongs to a voided player
		if (this.voidedObjectIds.Contains(characterPtr->GameObject.OwnerID))
		{
			this.HideGameObject(characterPtr);
			return;
		}

		if (!VisibilityPlugin.Instance.Configuration.Enabled ||
		    !VisibilityPlugin.Instance.Configuration.CurrentConfig.HideChocobo ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowFriendChocobo &&
		     this.containers[UnitType.Players][ContainerType.Friend]
			     .Contains(characterPtr->GameObject.OwnerID)) ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowCompanyChocobo &&
		     this.containers[UnitType.Players][ContainerType.Company]
			     .Contains(characterPtr->GameObject.OwnerID)) ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowPartyChocobo &&
		     this.containers[UnitType.Players][ContainerType.Party]
			     .Contains(characterPtr->GameObject.OwnerID)) ||
		    this.whitelistedObjectIds.Contains(characterPtr->GameObject.OwnerID))
		{
			return;
		}

		this.HideGameObject(characterPtr);
	}

	private unsafe void MinionHandler(Companion* companionPtr, Character* localPlayer)
	{
		var characterPtr = (Character*)companionPtr;
		if (localPlayer == null ||
		    characterPtr->CompanionOwnerID == localPlayer->GameObject.ObjectID ||
		    this.ShowGameObject(characterPtr, ObjectType.Companion))
		{
			return;
		}

		if (!VisibilityPlugin.Instance.Configuration.Enabled ||
		    !VisibilityPlugin.Instance.Configuration.CurrentConfig.HideMinion)
		{
			return;
		}

		this.containers[UnitType.Minions][ContainerType.All].Add(characterPtr->CompanionOwnerID);

		if (this.containers[UnitType.Players][ContainerType.Friend]
		    .Contains(characterPtr->CompanionOwnerID))
		{
			this.containers[UnitType.Minions][ContainerType.Friend].Add(characterPtr->CompanionOwnerID);
		}

		if (this.containers[UnitType.Players][ContainerType.Party]
		    .Contains(characterPtr->CompanionOwnerID))
		{
			this.containers[UnitType.Minions][ContainerType.Party].Add(characterPtr->CompanionOwnerID);
		}

		if (this.containers[UnitType.Players][ContainerType.Company]
		    .Contains(characterPtr->CompanionOwnerID))
		{
			this.containers[UnitType.Minions][ContainerType.Company].Add(characterPtr->CompanionOwnerID);
		}

		if ((VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowFriendMinion &&
		     this.containers[UnitType.Players][ContainerType.Friend]
			     .Contains(characterPtr->CompanionOwnerID))
		    || (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowCompanyMinion &&
		        this.containers[UnitType.Players][ContainerType.Company]
			        .Contains(characterPtr->CompanionOwnerID))
		    || (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowPartyMinion &&
		        this.containers[UnitType.Players][ContainerType.Party]
			        .Contains(characterPtr->CompanionOwnerID)))
		{
			return;
		}

		this.HideGameObject(characterPtr, ObjectType.Companion);
	}

	private unsafe void HideGameObject(Character* thisPtr, ObjectType objectType = ObjectType.Character)
	{
		switch (objectType)
		{
			case ObjectType.Character when !thisPtr->GameObject.RenderFlags.TestFlag(VisibilityFlags.Invisible):
				this.hiddenObjectIds.Add(thisPtr->GameObject.ObjectID);
				thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
				break;
			case ObjectType.Companion when !thisPtr->GameObject.RenderFlags.TestFlag(VisibilityFlags.Invisible):
				this.hiddenMinionObjectIds.Add(thisPtr->CompanionOwnerID);
				thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
				break;
		}
	}

	private unsafe bool ShowGameObject(Character* thisPtr, ObjectType objectType = ObjectType.Character)
	{
		switch (objectType)
		{
			case ObjectType.Character when this.objectIdsToShow.Contains(thisPtr->GameObject.ObjectID) &&
			                               thisPtr->GameObject.RenderFlags.TestFlag(VisibilityFlags.Invisible):
				this.hiddenObjectIds.Remove(thisPtr->GameObject.ObjectID);
				this.objectIdsToShow.Remove(thisPtr->GameObject.ObjectID);
				thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
				return true;
			case ObjectType.Companion when this.minionObjectIdsToShow.Contains(thisPtr->CompanionOwnerID) &&
			                               thisPtr->GameObject.RenderFlags.TestFlag(VisibilityFlags.Invisible):
				this.hiddenMinionObjectIds.Remove(thisPtr->CompanionOwnerID);
				this.minionObjectIdsToShow.Remove(thisPtr->CompanionOwnerID);
				thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
				return true;
		}

		return false;
	}

	private static unsafe bool IsObjectIdInParty(uint objectId)
	{
		var groupManager = GroupManager.Instance();
		var infoProxyCrossRealm = InfoProxyCrossRealm.Instance();

		if (groupManager->MemberCount > 0 && groupManager->IsObjectIDInParty(objectId))
		{
			return true;
		}

		if (infoProxyCrossRealm->IsInCrossRealmParty == 0)
		{
			return false;
		}

		foreach (var group in infoProxyCrossRealm->CrossRealmGroupSpan)
		{
			if (group.GroupMemberCount == 0)
			{
				continue;
			}

			for (var i = 0; i < group.GroupMemberCount; ++i)
			{
				if (group.GroupMemberSpan[i].ObjectId == objectId)
				{
					return true;
				}
			}
		}

		return false;
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

		this.containers[unitType][containerType].Clear();
	}

	public void OnTerritoryChanged()
	{
		this.isChangingTerritory = true;
		this.hiddenObjectIds.Clear();
		this.objectIdsToShow.Clear();
		this.hiddenMinionObjectIds.Clear();
		this.minionObjectIdsToShow.Clear();
		this.checkedVoidedObjectIds.Clear();
		this.checkedWhitelistedObjectIds.Clear();
		this.voidedObjectIds.Clear();
		this.whitelistedObjectIds.Clear();

		foreach (var (_, unitContainer) in this.containers)
		{
			foreach (var (_, container) in unitContainer)
			{
				container.Clear();
			}
		}

		this.isChangingTerritory = false;
	}

	public void ShowPlayers(ContainerType type) => this.Show(UnitType.Players, type);

	public void ShowPets(ContainerType type) => this.Show(UnitType.Pets, type);

	public void ShowChocobos(ContainerType type) => this.Show(UnitType.Chocobos, type);

	public void ShowMinions(ContainerType type) => this.Show(UnitType.Minions, type);

	public unsafe void ShowAll()
	{
		if (VisibilityPlugin.ClientState.LocalPlayer == null)
		{
			return;
		}

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
				this.minionObjectIdsToShow.Add(thisPtr->CompanionOwnerID);
				this.minionObjectIdsToShow.Remove(thisPtr->CompanionOwnerID);
			}
			else
			{
				if (!this.hiddenObjectIds.Contains(thisPtr->GameObject.ObjectID))
				{
					continue;
				}

				thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
				this.RemoveChecked(thisPtr->GameObject.ObjectID);
				this.objectIdsToShow.Add(thisPtr->GameObject.ObjectID);
				this.hiddenObjectIds.Remove(thisPtr->GameObject.ObjectID);
			}
		}
	}

	public void RemoveChecked(uint id)
	{
		this.voidedObjectIds.Remove(id);
		this.whitelistedObjectIds.Remove(id);
		this.checkedVoidedObjectIds.Remove(id);
		this.checkedWhitelistedObjectIds.Remove(id);
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

	public void Dispose()
	{
		this.ShowAll();
	}
}