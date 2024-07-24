using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Visibility.Void;

using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

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

public class FrameworkHandler: IDisposable
{
	private enum ObjectType
	{
		Character,
		Companion
	}

	private readonly Dictionary<uint, long> hiddenObjectIds = new(capacity: 200);
	private readonly Dictionary<uint, long> objectIdsToShow = new(capacity: 200);
	private readonly Dictionary<uint, long> checkedVoidedObjectIds = new(capacity: 1000);
	private readonly Dictionary<uint, long> checkedWhitelistedObjectIds = new(capacity: 1000);
	private readonly Dictionary<uint, long> voidedObjectIds = new(capacity: 1000);
	private readonly Dictionary<uint, long> whitelistedObjectIds = new(capacity: 1000);

	private readonly Dictionary<UnitType, Dictionary<ContainerType, Dictionary<uint, long>>> containers = new()
	{
		{
			UnitType.Players, new Dictionary<ContainerType, Dictionary<uint, long>>
			{
				{ ContainerType.All, new Dictionary<uint, long>() },
				{ ContainerType.Friend, new Dictionary<uint, long>() },
				{ ContainerType.Party, new Dictionary<uint, long>() },
				{ ContainerType.Company, new Dictionary<uint, long>() },
			}
		},
		{
			UnitType.Pets, new Dictionary<ContainerType, Dictionary<uint, long>>
			{
				{ ContainerType.All, new Dictionary<uint, long>() },
				{ ContainerType.Friend, new Dictionary<uint, long>() },
				{ ContainerType.Party, new Dictionary<uint, long>() },
				{ ContainerType.Company, new Dictionary<uint, long>() },
			}
		},
		{
			UnitType.Chocobos, new Dictionary<ContainerType, Dictionary<uint, long>>
			{
				{ ContainerType.All, new Dictionary<uint, long>() },
				{ ContainerType.Friend, new Dictionary<uint, long>() },
				{ ContainerType.Party, new Dictionary<uint, long>() },
				{ ContainerType.Company, new Dictionary<uint, long>() },
			}
		},
		{
			UnitType.Minions, new Dictionary<ContainerType, Dictionary<uint, long>>
			{
				{ ContainerType.All, new Dictionary<uint, long>() },
				{ ContainerType.Friend, new Dictionary<uint, long>() },
				{ ContainerType.Party, new Dictionary<uint, long>() },
				{ ContainerType.Company, new Dictionary<uint, long>() },
			}
		},
	};

	private readonly Dictionary<uint, long> hiddenMinionObjectIds = new(capacity: 200);
	private readonly Dictionary<uint, long> minionObjectIdsToShow = new(capacity: 200);
	private bool isChangingTerritory;

	private readonly HashSet<uint> idToDelete = new(capacity: 200);

	public unsafe void Update()
	{
		GameObject* localPlayerGameObject = GameObjectManager.Instance()->Objects.IndexSorted[0];
		IntPtr namePlateWidget = Service.GameGui.GetAddonByName("NamePlate");

		if (namePlateWidget == nint.Zero ||
		    (!((AtkUnitBase*)namePlateWidget)->IsVisible && !Service.Condition[ConditionFlag.Performing]) ||
		    localPlayerGameObject == null || localPlayerGameObject->EntityId == 0xE0000000 ||
		    VisibilityPlugin.Instance.Disable || this.isChangingTerritory)
		{
			return;
		}

		bool isBound = (Service.Condition[ConditionFlag.BoundByDuty] &&
		                localPlayerGameObject->EventId.ContentId != EventHandlerType.TreasureHuntDirector)
		               || Service.Condition[ConditionFlag.BetweenAreas]
		               || Service.Condition[ConditionFlag.WatchingCutscene]
		               || Service.Condition[ConditionFlag.DutyRecorderPlayback];

		Character* localPlayer = (Character*)localPlayerGameObject;

		for (int i = 1; i != 200; ++i)
		{
			GameObject* gameObject = GameObjectManager.Instance()->Objects.IndexSorted[i];
			Character* characterPtr = (Character*)gameObject;

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
				                               characterPtr->NameId != 6565:
					this.PetHandler(characterPtr, localPlayer, isBound);
					break;
				case ObjectKind.BattleNpc
					when characterPtr->GameObject.SubKind == (byte)BattleNpcSubKind.Pet && characterPtr->NameId == 6565
					: // Earthly Star
					{
						if (VisibilityPlugin.Instance.Configuration is { Enabled: true, HideStar: true }
						    && Service.Condition[ConditionFlag.InCombat]
						    && characterPtr->GameObject.OwnerId != localPlayer->GameObject.EntityId
						    && !this.containers[UnitType.Players][ContainerType.Party]
							    .ContainsKey(characterPtr->GameObject.OwnerId))
						{
							this.HideGameObject(characterPtr);
						}

						break;
					}
				case ObjectKind.BattleNpc when characterPtr->GameObject.SubKind == (byte)BattleNpcSubKind.Chocobo:
					this.ChocoboHandler(characterPtr, localPlayer);
					break;
				case ObjectKind.Companion:
					this.MinionHandler(characterPtr, localPlayer);
					break;
			}
		}

		foreach ((UnitType _, Dictionary<ContainerType, Dictionary<uint, long>>? unitContainer) in this.containers)
		{
			foreach ((ContainerType _, Dictionary<uint, long>? container) in unitContainer)
			{
				foreach ((uint id, long ticks) in container)
				{
					if (ticks > Environment.TickCount64 + 5000)
					{
						this.idToDelete.Add(id);
					}
				}

				foreach (uint id in this.idToDelete)
				{
					container.Remove(id);
				}

				this.idToDelete.Clear();
			}
		}
	}

	private unsafe void PlayerHandler(Character* characterPtr, Character* localPlayer, bool isBound)
	{
		if (characterPtr->GameObject.EntityId == 0xE0000000 ||
		    this.ShowGameObject(characterPtr))
		{
			return;
		}

		this.containers[UnitType.Players][ContainerType.All][characterPtr->GameObject.EntityId] =
			Environment.TickCount64;

		if (characterPtr->IsFriend)
		{
			this.containers[UnitType.Players][ContainerType.Friend][characterPtr->GameObject.EntityId] =
				Environment.TickCount64;
		}
		else
		{
			this.containers[UnitType.Players][ContainerType.Friend]
				.Remove(characterPtr->GameObject.EntityId);
		}

		bool isObjectIdInParty = IsObjectIdInParty(characterPtr->GameObject.EntityId);

		if (isObjectIdInParty)
		{
			this.containers[UnitType.Players][ContainerType.Party][characterPtr->GameObject.EntityId] =
				Environment.TickCount64;
		}
		else
		{
			this.containers[UnitType.Players][ContainerType.Party].Remove(characterPtr->GameObject.EntityId);
		}

		if (isBound && !VisibilityPlugin.Instance.Configuration.TerritoryTypeWhitelist.Contains(
			    Service.ClientState.TerritoryType))
		{
			return;
		}

		if (localPlayer->FreeCompanyTag[0] != 0
		    && localPlayer->CurrentWorld == localPlayer->HomeWorld
		    && characterPtr->FreeCompanyTag.SequenceEqual(localPlayer->FreeCompanyTag))
		{
			this.containers[UnitType.Players][ContainerType.Company][characterPtr->GameObject.EntityId] =
				Environment.TickCount64;
		}
		else
		{
			this.containers[UnitType.Players][ContainerType.Company]
				.Remove(characterPtr->GameObject.EntityId);
		}

		if (!this.checkedVoidedObjectIds.ContainsKey(characterPtr->GameObject.EntityId))
		{
			VoidItem? voidedPlayer = VisibilityPlugin.Instance.Configuration.VoidList.Find(
				x => characterPtr->GameObject.Name.SequenceEqual(x.NameBytes) &&
				     x.HomeworldId == characterPtr->HomeWorld);

			if (voidedPlayer != null)
			{
				voidedPlayer.ObjectId = characterPtr->GameObject.EntityId;
				this.voidedObjectIds[characterPtr->GameObject.EntityId] = Environment.TickCount64;
			}

			this.checkedVoidedObjectIds[characterPtr->GameObject.EntityId] = Environment.TickCount64;
		}

		if (this.voidedObjectIds.ContainsKey(characterPtr->GameObject.EntityId))
		{
			this.HideGameObject(characterPtr);
			return;
		}

		if (((VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowDeadPlayer &&
		      characterPtr->GameObject.IsDead()) ||
		     (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowPartyPlayer && isObjectIdInParty) ||
		     (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowFriendPlayer && characterPtr->IsFriend)) &&
		    this.hiddenObjectIds.ContainsKey(characterPtr->GameObject.EntityId))
		{
			characterPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
			this.hiddenObjectIds.Remove(characterPtr->GameObject.EntityId);
			return;
		}

		if (!VisibilityPlugin.Instance.Configuration.Enabled ||
		    !VisibilityPlugin.Instance.Configuration.CurrentConfig.HidePlayer ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowDeadPlayer &&
		     characterPtr->GameObject.IsDead()) ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowFriendPlayer &&
		     this.containers[UnitType.Players][ContainerType.Friend]
			     .ContainsKey(characterPtr->GameObject.EntityId)) ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowCompanyPlayer &&
		     this.containers[UnitType.Players][ContainerType.Company]
			     .ContainsKey(characterPtr->GameObject.EntityId)) ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowPartyPlayer &&
		     this.containers[UnitType.Players][ContainerType.Party]
			     .ContainsKey(characterPtr->GameObject.EntityId)))
		{
			return;
		}

		if (!this.checkedWhitelistedObjectIds.ContainsKey(characterPtr->GameObject.EntityId))
		{
			VoidItem? whitelistedPlayer = VisibilityPlugin.Instance.Configuration.Whitelist.Find(
				x => characterPtr->GameObject.Name.SequenceEqual(x.NameBytes) &&
				     x.HomeworldId == characterPtr->HomeWorld);

			if (whitelistedPlayer != null)
			{
				whitelistedPlayer.ObjectId = characterPtr->GameObject.EntityId;
				this.whitelistedObjectIds[characterPtr->GameObject.EntityId] = Environment.TickCount64;
			}

			this.checkedWhitelistedObjectIds[characterPtr->GameObject.EntityId] = Environment.TickCount64;
		}

		if (this.whitelistedObjectIds.ContainsKey(characterPtr->GameObject.EntityId))
		{
			return;
		}

		this.HideGameObject(characterPtr);
	}

	private unsafe void PetHandler(Character* characterPtr, Character* localPlayer, bool isBound)
	{
		// Ignore own pet
		if (characterPtr->GameObject.OwnerId == localPlayer->GameObject.EntityId ||
		    this.ShowGameObject(characterPtr))
		{
			return;
		}

		this.containers[UnitType.Pets][ContainerType.All][characterPtr->GameObject.EntityId] = Environment.TickCount64;

		if (this.containers[UnitType.Players][ContainerType.Friend]
		    .ContainsKey(characterPtr->GameObject.OwnerId))
		{
			this.containers[UnitType.Pets][ContainerType.Friend][characterPtr->GameObject.EntityId] =
				Environment.TickCount64;
		}

		if (this.containers[UnitType.Players][ContainerType.Party]
		    .ContainsKey(characterPtr->GameObject.OwnerId))
		{
			this.containers[UnitType.Pets][ContainerType.Party][characterPtr->GameObject.EntityId] =
				Environment.TickCount64;
		}

		if (this.containers[UnitType.Players][ContainerType.Company]
		    .ContainsKey(characterPtr->GameObject.OwnerId))
		{
			this.containers[UnitType.Pets][ContainerType.Company][characterPtr->GameObject.EntityId] =
				Environment.TickCount64;
		}

		// Do not hide pets in duties
		if (isBound)
		{
			return;
		}

		// Hide pet if it belongs to a voided player
		if (this.voidedObjectIds.ContainsKey(characterPtr->GameObject.OwnerId))
		{
			this.HideGameObject(characterPtr);
			return;
		}

		if (!VisibilityPlugin.Instance.Configuration.Enabled ||
		    !VisibilityPlugin.Instance.Configuration.CurrentConfig.HidePet ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowFriendPet &&
		     this.containers[UnitType.Players][ContainerType.Friend]
			     .ContainsKey(characterPtr->GameObject.OwnerId)) ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowCompanyPet &&
		     this.containers[UnitType.Players][ContainerType.Company]
			     .ContainsKey(characterPtr->GameObject.OwnerId)) ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowPartyPet &&
		     this.containers[UnitType.Players][ContainerType.Party]
			     .ContainsKey(characterPtr->GameObject.OwnerId)) ||
		    this.whitelistedObjectIds.ContainsKey(characterPtr->GameObject.OwnerId))
		{
			return;
		}

		this.HideGameObject(characterPtr);
	}

	private unsafe void ChocoboHandler(Character* characterPtr, Character* localPlayer)
	{
		// Ignore own chocobo
		if (characterPtr->GameObject.OwnerId == localPlayer->GameObject.EntityId ||
		    this.ShowGameObject(characterPtr))
		{
			return;
		}

		this.containers[UnitType.Chocobos][ContainerType.All][characterPtr->GameObject.EntityId] =
			Environment.TickCount64;

		if (this.containers[UnitType.Players][ContainerType.Friend]
		    .ContainsKey(characterPtr->GameObject.OwnerId))
		{
			this.containers[UnitType.Chocobos][ContainerType.Friend][characterPtr->GameObject.EntityId] =
				Environment.TickCount64;
		}

		if (this.containers[UnitType.Players][ContainerType.Party]
		    .ContainsKey(characterPtr->GameObject.OwnerId))
		{
			this.containers[UnitType.Chocobos][ContainerType.Party][characterPtr->GameObject.EntityId] =
				Environment.TickCount64;
		}

		if (this.containers[UnitType.Players][ContainerType.Company]
		    .ContainsKey(characterPtr->GameObject.OwnerId))
		{
			this.containers[UnitType.Chocobos][ContainerType.Company][characterPtr->GameObject.EntityId] =
				Environment.TickCount64;
		}

		// Hide chocobo if it belongs to a voided player
		if (this.voidedObjectIds.ContainsKey(characterPtr->GameObject.OwnerId))
		{
			this.HideGameObject(characterPtr);
			return;
		}

		if (!VisibilityPlugin.Instance.Configuration.Enabled ||
		    !VisibilityPlugin.Instance.Configuration.CurrentConfig.HideChocobo ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowFriendChocobo &&
		     this.containers[UnitType.Players][ContainerType.Friend]
			     .ContainsKey(characterPtr->GameObject.OwnerId)) ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowCompanyChocobo &&
		     this.containers[UnitType.Players][ContainerType.Company]
			     .ContainsKey(characterPtr->GameObject.OwnerId)) ||
		    (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowPartyChocobo &&
		     this.containers[UnitType.Players][ContainerType.Party]
			     .ContainsKey(characterPtr->GameObject.OwnerId)) ||
		    this.whitelistedObjectIds.ContainsKey(characterPtr->GameObject.OwnerId))
		{
			return;
		}

		this.HideGameObject(characterPtr);
	}

	private unsafe void MinionHandler(Character* characterPtr, Character* localPlayer)
	{
		if (localPlayer == null ||
		    characterPtr->CompanionOwnerId == localPlayer->GameObject.EntityId ||
		    this.ShowGameObject(characterPtr, ObjectType.Companion))
		{
			return;
		}

		if (!VisibilityPlugin.Instance.Configuration.Enabled ||
		    !VisibilityPlugin.Instance.Configuration.CurrentConfig.HideMinion)
		{
			return;
		}

		this.containers[UnitType.Minions][ContainerType.All][characterPtr->CompanionOwnerId] = Environment.TickCount64;

		if (this.containers[UnitType.Players][ContainerType.Friend]
		    .ContainsKey(characterPtr->CompanionOwnerId))
		{
			this.containers[UnitType.Minions][ContainerType.Friend][characterPtr->CompanionOwnerId] =
				Environment.TickCount64;
		}

		if (this.containers[UnitType.Players][ContainerType.Party]
		    .ContainsKey(characterPtr->CompanionOwnerId))
		{
			this.containers[UnitType.Minions][ContainerType.Party][characterPtr->CompanionOwnerId] =
				Environment.TickCount64;
		}

		if (this.containers[UnitType.Players][ContainerType.Company]
		    .ContainsKey(characterPtr->CompanionOwnerId))
		{
			this.containers[UnitType.Minions][ContainerType.Company][characterPtr->CompanionOwnerId] =
				Environment.TickCount64;
		}

		if ((VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowFriendMinion &&
		     this.containers[UnitType.Players][ContainerType.Friend]
			     .ContainsKey(characterPtr->CompanionOwnerId))
		    || (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowCompanyMinion &&
		        this.containers[UnitType.Players][ContainerType.Company]
			        .ContainsKey(characterPtr->CompanionOwnerId))
		    || (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowPartyMinion &&
		        this.containers[UnitType.Players][ContainerType.Party]
			        .ContainsKey(characterPtr->CompanionOwnerId)))
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
				this.hiddenObjectIds[thisPtr->GameObject.EntityId] = Environment.TickCount64;
				thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
				break;
			case ObjectType.Companion when !thisPtr->GameObject.RenderFlags.TestFlag(VisibilityFlags.Invisible):
				this.hiddenMinionObjectIds[thisPtr->CompanionOwnerId] = Environment.TickCount64;
				thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
				break;
		}
	}

	private unsafe bool ShowGameObject(Character* thisPtr, ObjectType objectType = ObjectType.Character)
	{
		switch (objectType)
		{
			case ObjectType.Character when this.objectIdsToShow.ContainsKey(thisPtr->GameObject.EntityId) &&
			                               thisPtr->GameObject.RenderFlags.TestFlag(VisibilityFlags.Invisible):
				this.hiddenObjectIds.Remove(thisPtr->GameObject.EntityId);
				this.objectIdsToShow.Remove(thisPtr->GameObject.EntityId);
				thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
				return true;
			case ObjectType.Companion when this.minionObjectIdsToShow.ContainsKey(thisPtr->CompanionOwnerId) &&
			                               thisPtr->GameObject.RenderFlags.TestFlag(VisibilityFlags.Invisible):
				this.hiddenMinionObjectIds.Remove(thisPtr->CompanionOwnerId);
				this.minionObjectIdsToShow.Remove(thisPtr->CompanionOwnerId);
				thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
				return true;
		}

		return false;
	}

	private static unsafe bool IsObjectIdInParty(uint objectId)
	{
		GroupManager* groupManager = GroupManager.Instance();
		InfoProxyCrossRealm* infoProxyCrossRealm = InfoProxyCrossRealm.Instance();

		if (groupManager->MainGroup.MemberCount > 0 && groupManager->MainGroup.IsEntityIdInParty(objectId))
		{
			return true;
		}

		if (infoProxyCrossRealm->IsInCrossRealmParty == 0)
		{
			return false;
		}

		foreach (CrossRealmGroup group in infoProxyCrossRealm->CrossRealmGroups)
		{
			if (group.GroupMembers.Length == 0)
			{
				continue;
			}

			for (int i = 0; i < group.GroupMembers.Length; ++i)
			{
				if (group.GroupMembers[i].EntityId == objectId)
				{
					return true;
				}
			}
		}

		return false;
	}

	public void Show(UnitType unitType, ContainerType containerType)
	{
		if (unitType == UnitType.Minions)
		{
			this.containers[unitType][containerType].ToList().ForEach(
				x =>
				{
					this.minionObjectIdsToShow[x.Key] = x.Value;
					this.hiddenMinionObjectIds.Remove(x.Key);
				});
		}
		else
		{
			this.containers[unitType][containerType].ToList().ForEach(
				x =>
				{
					this.objectIdsToShow[x.Key] = x.Value;
					this.hiddenObjectIds.Remove(x.Key);
				});
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

		foreach ((UnitType _, Dictionary<ContainerType, Dictionary<uint, long>>? unitContainer) in this.containers)
		{
			foreach ((ContainerType _, Dictionary<uint, long>? container) in unitContainer)
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
		if (Service.ClientState.LocalPlayer == null)
		{
			return;
		}

		foreach (Dalamud.Game.ClientState.Objects.Types.IGameObject? actor in Service.ObjectTable)
		{
			Character* thisPtr = (Character*)actor.Address;

			if ((byte)thisPtr->GameObject.ObjectKind == (byte)ObjectKind.Companion)
			{
				if (!this.hiddenMinionObjectIds.ContainsKey(thisPtr->CompanionOwnerId))
				{
					continue;
				}

				this.minionObjectIdsToShow[thisPtr->CompanionOwnerId] = Environment.TickCount64;
				this.hiddenMinionObjectIds.Remove(thisPtr->CompanionOwnerId);
			}
			else
			{
				if (!this.hiddenObjectIds.ContainsKey(thisPtr->GameObject.EntityId))
				{
					continue;
				}

				this.RemoveChecked(thisPtr->GameObject.EntityId);
				this.objectIdsToShow[thisPtr->GameObject.EntityId] = Environment.TickCount64;
				this.hiddenObjectIds.Remove(thisPtr->GameObject.EntityId);
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
		if (!this.hiddenObjectIds.ContainsKey(id))
		{
			return;
		}

		this.objectIdsToShow[id] = Environment.TickCount64;
		this.hiddenObjectIds.Remove(id);
	}

	public void Dispose() => this.ShowAll();
}
