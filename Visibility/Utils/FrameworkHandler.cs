using System;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Visibility.Utils.EntityHandlers;

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

/// <summary>
/// Main handler for managing visibility of game objects
/// </summary>
public class FrameworkHandler: IDisposable
{
	// Component managers
	private readonly ContainerManager containerManager;
	private readonly VoidListManager voidListManager;
	private readonly ObjectVisibilityManager visibilityManager;

	// Entity handlers
	private readonly PlayerHandler playerHandler;
	private readonly PetHandler petHandler;
	private readonly ChocoboHandler chocoboHandler;
	private readonly MinionHandler minionHandler;

	private bool isChangingTerritory;

	/// <summary>
	/// Constructor for FrameworkHandler
	/// </summary>
	public FrameworkHandler()
	{
		// Initialize managers
		this.containerManager = new ContainerManager();
		this.voidListManager = new VoidListManager();
		this.visibilityManager = new ObjectVisibilityManager();

		// Initialize entity handlers
		this.playerHandler = new PlayerHandler(this.containerManager, this.voidListManager, this.visibilityManager);
		this.petHandler = new PetHandler(this.containerManager, this.voidListManager, this.visibilityManager);
		this.chocoboHandler = new ChocoboHandler(this.containerManager, this.voidListManager, this.visibilityManager);
		this.minionHandler = new MinionHandler(this.containerManager, this.visibilityManager);
	}

	/// <summary>
	/// Main update method called by the framework
	/// </summary>
	public unsafe void Update()
	{
		// Get local player and check if we should process visibility
		GameObject* localPlayerGameObject = GameObjectManager.Instance()->Objects.IndexSorted[0];
		IntPtr namePlateWidget = Service.GameGui.GetAddonByName("NamePlate");

		// Early exit conditions
		if (namePlateWidget == nint.Zero ||
		    (!((AtkUnitBase*)namePlateWidget)->IsVisible && !Service.Condition[ConditionFlag.Performing]) ||
		    localPlayerGameObject == null || localPlayerGameObject->EntityId == 0xE0000000 ||
		    VisibilityPlugin.Instance.Disable || this.isChangingTerritory)
			return;

		// Check if player is in a duty or other special area
		bool isBound = (Service.Condition[ConditionFlag.BoundByDuty] &&
		                localPlayerGameObject->EventId.ContentId != EventHandlerContent.TreasureHuntDirector)
		               || Service.Condition[ConditionFlag.BetweenAreas]
		               || Service.Condition[ConditionFlag.WatchingCutscene]
		               || Service.Condition[ConditionFlag.DutyRecorderPlayback];

		Character* localPlayer = (Character*)localPlayerGameObject;

		// Process all game objects
		for (int i = 1; i != 200; ++i)
		{
			GameObject* gameObject = GameObjectManager.Instance()->Objects.IndexSorted[i];
			Character* characterPtr = (Character*)gameObject;

			if (gameObject == null || gameObject == localPlayerGameObject || !gameObject->IsCharacter()) continue;

			// Process different types of entities
			switch ((ObjectKind)characterPtr->GameObject.ObjectKind)
			{
				case ObjectKind.Player:
					this.playerHandler.ProcessPlayer(characterPtr, localPlayer, isBound);
					break;
				case ObjectKind.BattleNpc when characterPtr->GameObject.SubKind == (byte)BattleNpcSubKind.Pet &&
				                               characterPtr->NameId != 6565:
					this.petHandler.ProcessPet(characterPtr, localPlayer, isBound);
					break;
				case ObjectKind.BattleNpc
					when characterPtr->GameObject.SubKind == (byte)BattleNpcSubKind.Pet && characterPtr->NameId == 6565:
					// Earthly Star
					this.petHandler.ProcessEarthlyStar(characterPtr, localPlayer);
					break;
				case ObjectKind.BattleNpc when characterPtr->GameObject.SubKind == (byte)BattleNpcSubKind.Chocobo:
					this.chocoboHandler.ProcessChocobo(characterPtr, localPlayer);
					break;
				case ObjectKind.Companion:
					this.minionHandler.ProcessMinion(characterPtr, localPlayer);
					break;
			}
		}

		// Clean up expired entries in containers
		this.containerManager.CleanupContainers();
	}

	/// <summary>
	/// Check if a character is the target of the current target
	/// </summary>
	public static unsafe bool CheckTargetOfTarget(Character* ptr)
	{
		if (!VisibilityPlugin.Instance.Configuration.ShowTargetOfTarget) return false;

		Character* target = (Character*)TargetSystem.Instance()->Target;

		if (target == null || !target->IsCharacter()) return false;

		return CharacterManager.Instance()->LookupBattleCharaByEntityId(target->TargetId.ObjectId) == ptr;
	}

	/// <summary>
	/// Check if an object ID is in the player's party
	/// </summary>
	public static unsafe bool IsObjectIdInParty(uint objectId)
	{
		GroupManager* groupManager = GroupManager.Instance();
		InfoProxyCrossRealm* infoProxyCrossRealm = InfoProxyCrossRealm.Instance();

		// Check regular party
		if (groupManager->MainGroup.MemberCount > 0 && groupManager->MainGroup.IsEntityIdInParty(objectId)) return true;

		// Check cross-realm party
		if (!infoProxyCrossRealm->IsInCrossRealmParty) return false;

		foreach (CrossRealmGroup group in infoProxyCrossRealm->CrossRealmGroups)
		{
			if (group.GroupMembers.Length == 0) continue;

			for (int i = 0; i < group.GroupMembers.Length; ++i)
			{
				if (group.GroupMembers[i].EntityId == objectId)
					return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Show entities of a specific type and container
	/// </summary>
	public void Show(UnitType unitType, ContainerType containerType)
	{
		// Get all entities in the container
		IEnumerable<KeyValuePair<uint, long>> entities = this.containerManager.GetContainerEntities(unitType, containerType);

		// Mark each entity to be shown
		foreach (KeyValuePair<uint, long> entity in entities)
		{
			this.visibilityManager.MarkObjectToShow(entity.Key,
				unitType == UnitType.Minions
					? ObjectVisibilityManager.ObjectType.Companion
					: ObjectVisibilityManager.ObjectType.Character);
		}

		// Clear the container after processing
		this.containerManager.ClearContainer(unitType, containerType);
	}

	/// <summary>
	/// Handle territory change events
	/// </summary>
	public void OnTerritoryChanged()
	{
		this.isChangingTerritory = true;

		// Clear visibility states
		this.visibilityManager.ClearAll();

		// Clear void and whitelist states
		this.voidListManager.ClearAll();

		// Clear all containers
		this.containerManager.ClearAllContainers();

		this.isChangingTerritory = false;
	}

	/// <summary>
	/// Show all players in a specific container
	/// </summary>
	public void ShowPlayers(ContainerType type) => this.Show(UnitType.Players, type);

	/// <summary>
	/// Show all pets in a specific container
	/// </summary>
	public void ShowPets(ContainerType type) => this.Show(UnitType.Pets, type);

	/// <summary>
	/// Show all chocobos in a specific container
	/// </summary>
	public void ShowChocobos(ContainerType type) => this.Show(UnitType.Chocobos, type);

	/// <summary>
	/// Show all minions in a specific container
	/// </summary>
	public void ShowMinions(ContainerType type) => this.Show(UnitType.Minions, type);

	/// <summary>
	/// Show all hidden entities
	/// </summary>
	public unsafe void ShowAll()
	{
		if (Service.ClientState.LocalPlayer == null) return;

		// Process all game objects in the object table
		foreach (Dalamud.Game.ClientState.Objects.Types.IGameObject gameObject in Service.ObjectTable)
		{
			Character* thisPtr = (Character*)gameObject.Address;

			// Handle companions (minions)
			if ((byte)thisPtr->GameObject.ObjectKind == (byte)ObjectKind.Companion)
			{
				// Skip if not hidden
				if (!this.visibilityManager.IsObjectHidden(thisPtr->CompanionOwnerId,
					    ObjectVisibilityManager.ObjectType.Companion)) continue;

				// Mark to show
				this.visibilityManager.MarkObjectToShow(thisPtr->CompanionOwnerId,
					ObjectVisibilityManager.ObjectType.Companion);
			}
			else // Handle characters (players, pets, chocobos)
			{
				// Skip if not hidden
				if (!this.visibilityManager.IsObjectHidden(thisPtr->GameObject.EntityId)) continue;

				// Remove from void and whitelist checks
				this.voidListManager.RemoveChecked(thisPtr->GameObject.EntityId);

				// Mark to show
				this.visibilityManager.MarkObjectToShow(thisPtr->GameObject.EntityId);
			}
		}
	}

	/// <summary>
	/// Remove an entity from the checked lists
	/// </summary>
	public void RemoveChecked(uint id)
	{
		this.voidListManager.RemoveChecked(id);
	}

	/// <summary>
	/// Show a specific player by ID
	/// </summary>
	public void ShowPlayer(uint id)
	{
		if (!this.visibilityManager.IsObjectHidden(id)) return;

		this.visibilityManager.MarkObjectToShow(id);
	}

	/// <summary>
	/// Dispose the framework handler and show all hidden entities
	/// </summary>
	public void Dispose() => this.ShowAll();
}
