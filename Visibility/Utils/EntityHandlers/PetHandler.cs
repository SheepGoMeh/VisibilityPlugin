using Dalamud.Game.ClientState.Conditions;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Visibility.Configuration;

namespace Visibility.Utils.EntityHandlers;

/// <summary>
/// Handles visibility logic for pet entities
/// </summary>
public class PetHandler
{
	private readonly ContainerManager containerManager;
	private readonly VoidListManager voidListManager;
	private readonly ObjectVisibilityManager visibilityManager;

	private static VisibilityConfiguration Configuration => VisibilityPlugin.Instance.Configuration;
	private static TerritoryConfig CurrentConfig => VisibilityPlugin.Instance.Configuration.CurrentConfig;

	public PetHandler(
		ContainerManager containerManager,
		VoidListManager voidListManager,
		ObjectVisibilityManager visibilityManager)
	{
		this.containerManager = containerManager;
		this.voidListManager = voidListManager;
		this.visibilityManager = visibilityManager;
	}

	/// <summary>
	/// Process a pet entity and determine its visibility
	/// </summary>
	public unsafe void ProcessPet(Character* characterPtr, Character* localPlayer, bool isBound)
	{
		// Ignore own pet
		if (characterPtr->GameObject.OwnerId == localPlayer->GameObject.EntityId ||
		    this.visibilityManager.ShowGameObject(characterPtr)) return;

		// Add to containers
		this.UpdateContainers(characterPtr);

		// Do not hide pets in duties
		if (isBound) return;

		// Hide pet if it belongs to a voided player
		if (this.voidListManager.IsObjectVoided(characterPtr->GameObject.OwnerId))
		{
			this.visibilityManager.HideGameObject(characterPtr);
			return;
		}

		// Check visibility conditions
		if (this.ShouldShowPet(characterPtr))
		{
			this.visibilityManager.MarkObjectToShow(characterPtr->GameObject.EntityId);
			return;
		}

		this.visibilityManager.HideGameObject(characterPtr);
	}

	/// <summary>
	/// Update container memberships for the pet
	/// </summary>
	private unsafe void UpdateContainers(Character* characterPtr)
	{
		// All pets container
		this.containerManager.AddToContainer(UnitType.Pets, ContainerType.All, characterPtr->GameObject.EntityId);

		// Friend's pet container
		if (this.containerManager.IsInContainer(UnitType.Players, ContainerType.Friend,
			    characterPtr->GameObject.OwnerId))
		{
			this.containerManager.AddToContainer(UnitType.Pets, ContainerType.Friend,
				characterPtr->GameObject.EntityId);
		}

		// Party member's pet container
		if (this.containerManager.IsInContainer(UnitType.Players, ContainerType.Party,
			    characterPtr->GameObject.OwnerId))
			this.containerManager.AddToContainer(UnitType.Pets, ContainerType.Party, characterPtr->GameObject.EntityId);

		// Company member's pet container
		if (this.containerManager.IsInContainer(UnitType.Players, ContainerType.Company,
			    characterPtr->GameObject.OwnerId))
		{
			this.containerManager.AddToContainer(UnitType.Pets, ContainerType.Company,
				characterPtr->GameObject.EntityId);
		}
	}

	/// <summary>
	/// Determine if a pet should be shown based on configuration settings
	/// </summary>
	private unsafe bool ShouldShowPet(Character* characterPtr)
	{
		// Check if plugin is disabled or pet hiding is disabled
		if (!Configuration.Enabled ||
		    !CurrentConfig.HidePet)
			return true;

		// Check if pet's owner is a friend and show friends' pets is enabled
		if (CurrentConfig.ShowFriendPet &&
		    this.containerManager.IsInContainer(UnitType.Players, ContainerType.Friend,
			    characterPtr->GameObject.OwnerId)) return true;

		// Check if pet's owner is in the same company and show company members' pets is enabled
		if (CurrentConfig.ShowCompanyPet &&
		    this.containerManager.IsInContainer(UnitType.Players, ContainerType.Company,
			    characterPtr->GameObject.OwnerId)) return true;

		// Check if pet's owner is in the party and show party members' pets is enabled
		if (CurrentConfig.ShowPartyPet &&
		    this.containerManager.IsInContainer(UnitType.Players, ContainerType.Party,
			    characterPtr->GameObject.OwnerId)) return true;

		// Check if pet's owner is whitelisted
		if (this.voidListManager.IsObjectWhitelisted(characterPtr->GameObject.OwnerId))
			return true;

		// Check if local player is in combat and hide pets in combat is enabled
		return CurrentConfig is { HidePetInCombat: true, HidePet: false } &&
		       !Service.Condition[ConditionFlag.InCombat];
	}

	/// <summary>
	/// Process an Earthly Star (special pet type)
	/// </summary>
	public unsafe void ProcessEarthlyStar(Character* characterPtr, Character* localPlayer)
	{
		if (Configuration is { Enabled: true, HideStar: true }
		    && Service.Condition[ConditionFlag.InCombat]
		    && characterPtr->GameObject.OwnerId != localPlayer->GameObject.EntityId
		    && !this.containerManager.IsInContainer(UnitType.Players, ContainerType.Party,
			    characterPtr->GameObject.OwnerId))
			this.visibilityManager.HideGameObject(characterPtr);
	}
}
