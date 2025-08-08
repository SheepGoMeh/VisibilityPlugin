using System;

using Dalamud.Game.ClientState.Conditions;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Visibility.Configuration;

namespace Visibility.Utils.EntityHandlers;

/// <summary>
/// Handles visibility logic for player entities
/// </summary>
public class PlayerHandler
{
	private readonly ContainerManager containerManager;
	private readonly VoidListManager voidListManager;
	private readonly ObjectVisibilityManager visibilityManager;

	private static VisibilityConfiguration Configuration => VisibilityPlugin.Instance.Configuration;
	private static TerritoryConfig CurrentConfig => VisibilityPlugin.Instance.Configuration.CurrentConfig;

	public PlayerHandler(
		ContainerManager containerManager,
		VoidListManager voidListManager,
		ObjectVisibilityManager visibilityManager)
	{
		this.containerManager = containerManager;
		this.voidListManager = voidListManager;
		this.visibilityManager = visibilityManager;
	}

	/// <summary>
	/// Process a player entity and determine its visibility
	/// </summary>
	public unsafe void ProcessPlayer(Character* characterPtr, Character* localPlayer, bool isBound)
	{
		if (characterPtr->GameObject.EntityId == 0xE0000000 ||
		    this.visibilityManager.ShowGameObject(characterPtr)) return;

		// Add to containers
		this.UpdateContainers(characterPtr, localPlayer);

		// Check territory whitelist
		if (isBound && !Configuration.TerritoryTypeWhitelist.Contains(
			    Service.ClientState.TerritoryType))
			return;

		// Check void list
		if (this.voidListManager.CheckAndProcessVoidList(characterPtr))
		{
			this.visibilityManager.HideGameObject(characterPtr);
			return;
		}

		// Check whitelist
		if (this.voidListManager.CheckAndProcessWhitelist(characterPtr)) return;

		// Check visibility conditions
		if (this.ShouldShowPlayer(characterPtr))
		{
			this.visibilityManager.MarkObjectToShow(characterPtr->GameObject.EntityId);
			return;
		}

		this.visibilityManager.HideGameObject(characterPtr);
	}

	/// <summary>
	/// Update container memberships for the player
	/// </summary>
	private unsafe void UpdateContainers(Character* characterPtr, Character* localPlayer)
	{
		// All players container
		this.containerManager.AddToContainer(UnitType.Players, ContainerType.All, characterPtr->GameObject.EntityId);

		// Friend container
		if (characterPtr->IsFriend)
		{
			this.containerManager.AddToContainer(UnitType.Players, ContainerType.Friend,
				characterPtr->GameObject.EntityId);
		}
		else
		{
			this.containerManager.RemoveFromContainer(UnitType.Players, ContainerType.Friend,
				characterPtr->GameObject.EntityId);
		}

		// Party container
		bool isObjectIdInParty = FrameworkHandler.IsObjectIdInParty(characterPtr->GameObject.EntityId);
		if (isObjectIdInParty)
		{
			this.containerManager.AddToContainer(UnitType.Players, ContainerType.Party,
				characterPtr->GameObject.EntityId);
		}
		else
		{
			this.containerManager.RemoveFromContainer(UnitType.Players, ContainerType.Party,
				characterPtr->GameObject.EntityId);
		}

		// Company container
		if (localPlayer->FreeCompanyTag[0] != 0
		    && localPlayer->CurrentWorld == localPlayer->HomeWorld
		    && characterPtr->FreeCompanyTag.SequenceEqual(localPlayer->FreeCompanyTag))
		{
			this.containerManager.AddToContainer(UnitType.Players, ContainerType.Company,
				characterPtr->GameObject.EntityId);
		}
		else
		{
			this.containerManager.RemoveFromContainer(UnitType.Players, ContainerType.Company,
				characterPtr->GameObject.EntityId);
		}
	}

	/// <summary>
	/// Determine if a player should be shown based on configuration settings
	/// </summary>
	private unsafe bool ShouldShowPlayer(Character* characterPtr)
	{
		// Check if plugin is disabled or player hiding is disabled
		if (!Configuration.Enabled ||
		    !CurrentConfig.HidePlayer)
			return true;

		// Check if player is dead and show dead players is enabled
		if (CurrentConfig.ShowDeadPlayer &&
		    characterPtr->GameObject.IsDead())
			return true;

		// Check if player is a friend and show friends is enabled
		if (CurrentConfig.ShowFriendPlayer &&
		    this.containerManager.IsInContainer(UnitType.Players, ContainerType.Friend,
			    characterPtr->GameObject.EntityId)) return true;

		// Check if player is in the same company and show company members is enabled
		if (CurrentConfig.ShowCompanyPlayer &&
		    this.containerManager.IsInContainer(UnitType.Players, ContainerType.Company,
			    characterPtr->GameObject.EntityId)) return true;

		// Check if player is in the party and show party members is enabled
		if (CurrentConfig.ShowPartyPlayer &&
		    this.containerManager.IsInContainer(UnitType.Players, ContainerType.Party,
			    characterPtr->GameObject.EntityId)) return true;

		// Check if player is the target of the target
		if (FrameworkHandler.CheckTargetOfTarget(characterPtr))
			return true;

		// Check if local player is in combat and hide players in combat is enabled
		return CurrentConfig.HidePlayerInCombat &&
		       !Service.Condition[ConditionFlag.InCombat];
	}
}
