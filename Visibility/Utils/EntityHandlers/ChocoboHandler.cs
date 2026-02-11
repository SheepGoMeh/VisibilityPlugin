using Dalamud.Game.ClientState.Conditions;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Visibility.Configuration;

namespace Visibility.Utils.EntityHandlers;

/// <summary>
/// Handles visibility logic for chocobo entities
/// </summary>
public class ChocoboHandler
{
	private readonly ContainerManager containerManager;
	private readonly VoidListManager voidListManager;
	private readonly ObjectVisibilityManager visibilityManager;
	private readonly VisibilityConfiguration configuration;

	public ChocoboHandler(
		ContainerManager containerManager,
		VoidListManager voidListManager,
		ObjectVisibilityManager visibilityManager,
		VisibilityConfiguration configuration)
	{
		this.containerManager = containerManager;
		this.voidListManager = voidListManager;
		this.visibilityManager = visibilityManager;
		this.configuration = configuration;
	}

	/// <summary>
	/// Process a chocobo entity and determine its visibility
	/// </summary>
	public unsafe void ProcessChocobo(Character* characterPtr, Character* localPlayer)
	{
		// Ignore own chocobo
		if (characterPtr->GameObject.OwnerId == localPlayer->GameObject.EntityId ||
		    this.visibilityManager.ShowGameObject(characterPtr)) return;

		// Add to containers
		this.UpdateContainers(characterPtr);

		// Hide chocobo if it belongs to a voided player
		if (this.voidListManager.IsObjectVoided(characterPtr->GameObject.OwnerId))
		{
			this.visibilityManager.HideGameObject(characterPtr);
			return;
		}

		// Check visibility conditions
		if (this.ShouldShowChocobo(characterPtr))
		{
			this.visibilityManager.MarkObjectToShow(characterPtr->GameObject.EntityId);
			return;
		}

		this.visibilityManager.HideGameObject(characterPtr);
	}

	/// <summary>
	/// Update container memberships for the chocobo
	/// </summary>
	private unsafe void UpdateContainers(Character* characterPtr)
	{
		// All chocobos container
		this.containerManager.AddToContainer(UnitType.Chocobos, ContainerType.All, characterPtr->GameObject.EntityId);

		// Friend's chocobo container
		if (this.containerManager.IsInContainer(UnitType.Players, ContainerType.Friend,
			    characterPtr->GameObject.OwnerId))
		{
			this.containerManager.AddToContainer(UnitType.Chocobos, ContainerType.Friend,
				characterPtr->GameObject.EntityId);
		}

		// Party member's chocobo container
		if (this.containerManager.IsInContainer(UnitType.Players, ContainerType.Party,
			    characterPtr->GameObject.OwnerId))
		{
			this.containerManager.AddToContainer(UnitType.Chocobos, ContainerType.Party,
				characterPtr->GameObject.EntityId);
		}

		// Company member's chocobo container
		if (this.containerManager.IsInContainer(UnitType.Players, ContainerType.Company,
			    characterPtr->GameObject.OwnerId))
		{
			this.containerManager.AddToContainer(UnitType.Chocobos, ContainerType.Company,
				characterPtr->GameObject.EntityId);
		}
	}

	/// <summary>
	/// Determine if a chocobo should be shown based on configuration settings
	/// </summary>
	private unsafe bool ShouldShowChocobo(Character* characterPtr)
	{
		// Check if plugin is disabled or chocobo hiding is disabled
		TerritoryConfig currentConfig = this.configuration.CurrentConfig;

		if (!this.configuration.Enabled ||
		    !currentConfig.HideChocobo)
			return true;

		// Check if chocobo's owner is a friend and show friends' chocobos is enabled
		if (currentConfig.ShowFriendChocobo &&
		    this.containerManager.IsInContainer(UnitType.Players, ContainerType.Friend,
			    characterPtr->GameObject.OwnerId)) return true;

		// Check if chocobo's owner is in the same company and show company members' chocobos is enabled
		if (currentConfig.ShowCompanyChocobo &&
		    this.containerManager.IsInContainer(UnitType.Players, ContainerType.Company,
			    characterPtr->GameObject.OwnerId)) return true;

		// Check if chocobo's owner is in the party and show party members' chocobos is enabled
		if (currentConfig.ShowPartyChocobo &&
		    this.containerManager.IsInContainer(UnitType.Players, ContainerType.Party,
			    characterPtr->GameObject.OwnerId)) return true;

		// Check if chocobo's owner is whitelisted
		if (this.voidListManager.IsObjectWhitelisted(characterPtr->GameObject.OwnerId))
			return true;

		// Check if local player is in combat and hide chocobos in combat is enabled
		return currentConfig is { HideChocoboInCombat: true, HideChocobo: false } &&
		       !Service.Condition[ConditionFlag.InCombat];
	}
}
