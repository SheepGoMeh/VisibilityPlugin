using System;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Visibility.Void;
using Visibility.Configuration;

using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace Visibility.Utils;

public class FrameworkHandler: IDisposable
{
	private static VisibilityConfiguration Configuration => VisibilityPlugin.Instance.Configuration;
	private static TerritoryConfig CurrentConfig => VisibilityPlugin.Instance.Configuration.CurrentConfig;


	private readonly HashSet<long> hiddenObjectIds = new(capacity: 200);
	private readonly HashSet<long> objectIdsToShow = new(capacity: 200);
	private readonly HashSet<long> checkedVoidedObjectIds = new(capacity: 1000);
	private readonly HashSet<long> checkedWhitelistedObjectIds = new(capacity: 1000);
	private readonly HashSet<long> voidedObjectIds = new(capacity: 1000);
	private readonly HashSet<long> whitelistedObjectIds = new(capacity: 1000);
	
	private readonly UnitContainer playerUnitContainer = new();
	private readonly UnitContainer petUnitContainer = new();
	private readonly UnitContainer chocoboUnitContainer = new();
	private readonly UnitContainer companionUnitContainer = new();

	private readonly HashSet<long> hiddenMinionObjectIds = new(capacity: 200);
	private readonly HashSet<long> minionObjectIdsToShow = new(capacity: 200);
	private bool isChangingTerritory;

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
				case ObjectKind.BattleNpc when characterPtr->GameObject.SubKind == (byte)BattleNpcSubKind.Pet:
					this.PetHandler(characterPtr, localPlayer, isBound);
					break;
				case ObjectKind.BattleNpc when characterPtr->GameObject.SubKind == (byte)BattleNpcSubKind.Chocobo:
					this.ChocoboHandler(characterPtr, localPlayer);
					break;
				case ObjectKind.Companion:
					this.MinionHandler(characterPtr, localPlayer);
					break;
			}
		}
	}

	private static unsafe bool CheckTargetOfTarget(Character* ptr)
	{
		if (!Configuration.ShowTargetOfTarget)
		{
			return false;
		}

		Character* target = (Character*)TargetSystem.Instance()->Target;

		if (target == null || !target->IsCharacter())
		{
			return false;
		}

		return CharacterManager.Instance()->LookupBattleCharaByEntityId(target->TargetId.ObjectId) == ptr;
	}

	private unsafe void PlayerHandler(Character* characterPtr, Character* localPlayer, bool isBound)
	{
		uint entityId = characterPtr->GameObject.EntityId;
		if (entityId == 0xE0000000 || this.ShowGameObject(characterPtr))
		{
			return;
		}

		this.playerUnitContainer.AllUnitIds.Add(entityId);

		if (characterPtr->IsFriend)
		{
			this.playerUnitContainer.FriendUnitIds.Add(entityId);
		}
		else
		{
			this.playerUnitContainer.FriendUnitIds.Remove(entityId);
		}

		bool isObjectIdInParty = IsObjectIdInParty(entityId);

		if (isObjectIdInParty)
		{
			this.playerUnitContainer.PartyUnitIds.Add(entityId);
		}
		else
		{
			this.playerUnitContainer.PartyUnitIds.Remove(entityId);
		}

		if (isBound && !Configuration.TerritoryTypeWhitelist.Contains(
			    Service.ClientState.TerritoryType))
		{
			return;
		}

		if (localPlayer->FreeCompanyTag[0] != 0
		    && localPlayer->CurrentWorld == localPlayer->HomeWorld
		    && characterPtr->FreeCompanyTag.SequenceEqual(localPlayer->FreeCompanyTag))
		{
			this.playerUnitContainer.CompanyUnitIds.Add(entityId);
		}
		else
		{
			this.playerUnitContainer.CompanyUnitIds.Remove(entityId);
		}

		if (!this.checkedVoidedObjectIds.Contains(entityId))
		{
			if (!Configuration.VoidDictionary.TryGetValue(characterPtr->AccountId,
					out VoidItem? voidedPlayer))
			{
				voidedPlayer = Configuration.VoidList.Find(
					x => characterPtr->GameObject.Name.StartsWith(x.NameBytes) &&
						 x.HomeworldId == characterPtr->HomeWorld);
			}

			if (voidedPlayer != null)
			{
				if (voidedPlayer.Id == 0)
				{
					voidedPlayer.Id = characterPtr->AccountId;
					Configuration.Save();
					Configuration.VoidDictionary[characterPtr->AccountId] = voidedPlayer;
				}

				voidedPlayer.ObjectId = entityId;
				this.voidedObjectIds.Add(entityId);
			}

			this.checkedVoidedObjectIds.Add(entityId);
		}

		if (this.voidedObjectIds.Contains(entityId))
		{
			this.HideGameObject(characterPtr);
			return;
		}

		if (((CurrentConfig.ShowDeadPlayer && characterPtr->GameObject.IsDead()) ||
		     (CurrentConfig.ShowPartyPlayer && isObjectIdInParty) ||
		     (CurrentConfig.ShowFriendPlayer && characterPtr->IsFriend) ||
		     CheckTargetOfTarget(characterPtr)) && this.hiddenObjectIds.Contains(entityId))
		{
			characterPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
			this.hiddenObjectIds.Remove(entityId);
			return;
		}

		if (!Configuration.Enabled ||
		    !CurrentConfig.HidePlayer ||
		    (CurrentConfig.ShowDeadPlayer && characterPtr->GameObject.IsDead()) ||
		    (CurrentConfig.ShowFriendPlayer && this.playerUnitContainer.FriendUnitIds.Contains(entityId)) ||
		    (CurrentConfig.ShowCompanyPlayer && this.playerUnitContainer.CompanyUnitIds.Contains(entityId)) ||
		    (CurrentConfig.ShowPartyPlayer && this.playerUnitContainer.PartyUnitIds.Contains(entityId)) ||
		    CheckTargetOfTarget(characterPtr))
		{
			return;
		}

		if (!this.checkedWhitelistedObjectIds.Contains(entityId))
		{
			if (!Configuration.WhitelistDictionary.TryGetValue(characterPtr->ContentId,
				out VoidItem? whitelistedPlayer))
			{
				whitelistedPlayer = Configuration.Whitelist.Find(
					x => characterPtr->GameObject.Name.StartsWith(x.NameBytes) &&
						 x.HomeworldId == characterPtr->HomeWorld);
			}

			if (whitelistedPlayer != null)
			{
				if (whitelistedPlayer.Id == 0)
				{
					whitelistedPlayer.Id = characterPtr->ContentId;
					Configuration.Save();
					Configuration.WhitelistDictionary[characterPtr->ContentId] = whitelistedPlayer;
				}

				whitelistedPlayer.ObjectId = entityId;
				this.whitelistedObjectIds.Add(entityId);
			}

			this.checkedWhitelistedObjectIds.Add(entityId);
		}

		if (this.whitelistedObjectIds.Contains(entityId))
		{
			return;
		}

		this.HideGameObject(characterPtr);
	}

	private static void UpdateContainer(
		UnitContainer containerToCheck,
		long idToCheck,
		UnitContainer containerToUpdate,
		long idToUpdate)
	{
		if (containerToCheck.FriendUnitIds.Contains(idToCheck))
		{
			containerToUpdate.FriendUnitIds.Add(idToUpdate);
		}
		else
		{
			containerToUpdate.FriendUnitIds.Remove(idToUpdate);
		}
		
		if (containerToCheck.PartyUnitIds.Contains(idToCheck))
		{
			containerToUpdate.PartyUnitIds.Add(idToUpdate);
		}
		else
		{
			containerToUpdate.PartyUnitIds.Remove(idToUpdate);
		}
		
		if (containerToCheck.CompanyUnitIds.Contains(idToCheck))
		{
			containerToUpdate.CompanyUnitIds.Add(idToUpdate);
		}
		else
		{
			containerToUpdate.CompanyUnitIds.Remove(idToUpdate);
		}
	}

	private unsafe void PetHandler(Character* characterPtr, Character* localPlayer, bool isBound)
	{
		uint ownerId = characterPtr->GameObject.OwnerId;

		// Ignore own pet
		if (ownerId == localPlayer->GameObject.EntityId || this.ShowGameObject(characterPtr))
		{
			return;
		}

		// Handle the unique condition of Earthly Star pet
		if (characterPtr->NameId == 6565)
		{
			if (Configuration is { Enabled: true, HideStar: true } &&
			    Service.Condition[ConditionFlag.InCombat] &&
			    !this.playerUnitContainer.PartyUnitIds.Contains(ownerId))
			{
				this.HideGameObject(characterPtr);
			}
			
			return;
		}
		
		uint entityId = characterPtr->GameObject.EntityId;

		this.petUnitContainer.AllUnitIds.Add(entityId);
		
		UpdateContainer(this.playerUnitContainer, ownerId, this.petUnitContainer, entityId);

		// Do not hide pets in duties
		if (isBound)
		{
			return;
		}

		// Hide pet if it belongs to a voided player
		if (this.voidedObjectIds.Contains(ownerId))
		{
			this.HideGameObject(characterPtr);
			return;
		}

		if (!Configuration.Enabled ||
		    !CurrentConfig.HidePet ||
		    (CurrentConfig.ShowFriendPet && this.petUnitContainer.FriendUnitIds.Contains(entityId)) ||
		    (CurrentConfig.ShowCompanyPet && this.petUnitContainer.CompanyUnitIds.Contains(entityId)) ||
		    (CurrentConfig.ShowPartyPet && this.petUnitContainer.PartyUnitIds.Contains(entityId)) ||
		    this.whitelistedObjectIds.Contains(ownerId))
		{
			return;
		}

		this.HideGameObject(characterPtr);
	}

	private unsafe void ChocoboHandler(Character* characterPtr, Character* localPlayer)
	{
		uint ownerId = characterPtr->GameObject.OwnerId;

		// Ignore own chocobo
		if (ownerId == localPlayer->GameObject.EntityId || this.ShowGameObject(characterPtr))
		{
			return;
		}
		
		uint entityId = characterPtr->GameObject.EntityId;
		this.chocoboUnitContainer.AllUnitIds.Add(entityId);

		UpdateContainer(this.playerUnitContainer, ownerId, this.chocoboUnitContainer, entityId);

		// Hide chocobo if it belongs to a voided player
		if (this.voidedObjectIds.Contains(ownerId))
		{
			this.HideGameObject(characterPtr);
			return;
		}

		if (!Configuration.Enabled ||
		    !CurrentConfig.HideChocobo ||
		    (CurrentConfig.ShowFriendChocobo && this.chocoboUnitContainer.FriendUnitIds.Contains(entityId)) ||
		    (CurrentConfig.ShowCompanyChocobo && this.chocoboUnitContainer.CompanyUnitIds.Contains(entityId)) ||
		    (CurrentConfig.ShowPartyChocobo && this.chocoboUnitContainer.PartyUnitIds.Contains(entityId)) ||
		    this.whitelistedObjectIds.Contains(ownerId))
		{
			return;
		}

		this.HideGameObject(characterPtr);
	}

	private unsafe void MinionHandler(Character* characterPtr, Character* localPlayer)
	{
		uint ownerId = characterPtr->CompanionOwnerId;
		
		// Ignore own companion
		if (localPlayer == null || ownerId == localPlayer->GameObject.EntityId ||
		    this.ShowGameObject(characterPtr, true))
		{
			return;
		}

		if (!Configuration.Enabled ||
		    !CurrentConfig.HideMinion)
		{
			return;
		}
		
		this.companionUnitContainer.AllUnitIds.Add(ownerId);

		UpdateContainer(this.playerUnitContainer, ownerId, this.companionUnitContainer, ownerId);

		if ((CurrentConfig.ShowFriendMinion && this.companionUnitContainer.FriendUnitIds.Contains(ownerId))
		    || (CurrentConfig.ShowCompanyMinion && this.companionUnitContainer.CompanyUnitIds.Contains(ownerId))
		    || (CurrentConfig.ShowPartyMinion && this.companionUnitContainer.PartyUnitIds.Contains(ownerId)))
		{
			return;
		}

		this.HideGameObject(characterPtr, true);
	}

	private unsafe void HideGameObject(Character* thisPtr, bool isCompanion = false)
	{
		if (!thisPtr->GameObject.RenderFlags.TestFlag(VisibilityFlags.Invisible))
		{
			if (isCompanion)
			{
				this.hiddenMinionObjectIds.Add(thisPtr->CompanionOwnerId);
			}
			else
			{
				this.hiddenObjectIds.Add(thisPtr->GameObject.EntityId);
			}

			thisPtr->GameObject.RenderFlags |= (int)VisibilityFlags.Invisible;
		}
	}

	private unsafe bool ShowGameObject(Character* thisPtr, bool isCompanion = false)
	{
		if (!thisPtr->GameObject.RenderFlags.TestFlag(VisibilityFlags.Invisible))
		{
			return false;
		}

		if (isCompanion)
		{
			if (!this.minionObjectIdsToShow.Remove(thisPtr->CompanionOwnerId))
			{
				return false;
			}

			this.hiddenMinionObjectIds.Remove(thisPtr->CompanionOwnerId);
		}
		else
		{
			if (!this.objectIdsToShow.Remove(thisPtr->GameObject.EntityId))
			{
				return false;
			}

			this.hiddenObjectIds.Remove(thisPtr->GameObject.EntityId);
		}
			
		thisPtr->GameObject.RenderFlags &= ~(int)VisibilityFlags.Invisible;
		return true;

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

	private void Show(HashSet<long> objectIds, bool isCompanion)
	{
		foreach (long objectId in objectIds)
		{
			if (isCompanion)
			{
				if (this.hiddenMinionObjectIds.Remove(objectId))
				{
					this.minionObjectIdsToShow.Add(objectId);
				}
			}
			else
			{
				if (this.hiddenObjectIds.Remove(objectId))
				{
					this.objectIdsToShow.Add(objectId);
				}
			}
		}
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
		
		this.playerUnitContainer.ClearAllUnitIds();
		this.petUnitContainer.ClearAllUnitIds();
		this.chocoboUnitContainer.ClearAllUnitIds();

		this.isChangingTerritory = false;
	}

	public void ShowAllPlayers() => this.Show(this.playerUnitContainer.AllUnitIds, false);
	public void ShowFriendPlayers() => this.Show(this.playerUnitContainer.FriendUnitIds, false);
	public void ShowPartyPlayers() => this.Show(this.playerUnitContainer.PartyUnitIds, false);
	public void ShowCompanyPlayers() => this.Show(this.playerUnitContainer.CompanyUnitIds, false);
	
	public void ShowAllPets() => this.Show(this.petUnitContainer.AllUnitIds, false);
	public void ShowFriendPets() => this.Show(this.petUnitContainer.FriendUnitIds, false);
	public void ShowPartyPets() => this.Show(this.petUnitContainer.PartyUnitIds, false);
	public void ShowCompanyPets() => this.Show(this.petUnitContainer.CompanyUnitIds, false);

	public void ShowAllChocobos() => this.Show(this.chocoboUnitContainer.AllUnitIds, false);
	public void ShowFriendChocobos() => this.Show(this.chocoboUnitContainer.FriendUnitIds, false);
	public void ShowPartyChocobos() => this.Show(this.chocoboUnitContainer.PartyUnitIds, false);
	public void ShowCompanyChocobos() => this.Show(this.chocoboUnitContainer.CompanyUnitIds, false);

	public void ShowAllCompanions() => this.Show(this.companionUnitContainer.AllUnitIds, true);
	public void ShowFriendCompanions() => this.Show(this.companionUnitContainer.FriendUnitIds, true);
	public void ShowPartyCompanions() => this.Show(this.companionUnitContainer.PartyUnitIds, true);
	public void ShowCompanyCompanions() => this.Show(this.companionUnitContainer.CompanyUnitIds, true);


	public unsafe void ShowAll()
	{
		if (Service.ClientState.LocalPlayer == null)
		{
			return;
		}

		foreach (IGameObject actor in Service.ObjectTable)
		{
			Character* thisPtr = (Character*)actor.Address;

			if ((byte)thisPtr->GameObject.ObjectKind == (byte)ObjectKind.Companion)
			{
				if (this.hiddenMinionObjectIds.Remove(thisPtr->CompanionOwnerId))
				{
					this.minionObjectIdsToShow.Add(thisPtr->CompanionOwnerId);
				}
			}
			else
			{
				if (this.hiddenObjectIds.Remove(thisPtr->GameObject.EntityId))
				{
					this.RemoveChecked(thisPtr->GameObject.EntityId);
					this.objectIdsToShow.Add(thisPtr->GameObject.EntityId);
				}
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
		if (this.hiddenObjectIds.Remove(id))
		{
			this.objectIdsToShow.Add(id);
		}
	}

	public void Dispose() => this.ShowAll();
}
