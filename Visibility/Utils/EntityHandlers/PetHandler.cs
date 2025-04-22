using Dalamud.Game.ClientState.Conditions;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace Visibility.Utils.EntityHandlers;

/// <summary>
/// Handles visibility logic for pet entities
/// </summary>
public class PetHandler
{
	private readonly ContainerManager containerManager;
	private readonly VoidListManager voidListManager;
	private readonly ObjectVisibilityManager visibilityManager;

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
		if (this.ShouldShowPet(characterPtr)) return;

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
		if (!VisibilityPlugin.Instance.Configuration.Enabled ||
		    !VisibilityPlugin.Instance.Configuration.CurrentConfig.HidePet)
			return true;

		// Check if pet's owner is a friend and show friends' pets is enabled
		if (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowFriendPet &&
		    this.containerManager.IsInContainer(UnitType.Players, ContainerType.Friend,
			    characterPtr->GameObject.OwnerId)) return true;

		// Check if pet's owner is in the same company and show company members' pets is enabled
		if (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowCompanyPet &&
		    this.containerManager.IsInContainer(UnitType.Players, ContainerType.Company,
			    characterPtr->GameObject.OwnerId)) return true;

		// Check if pet's owner is in the party and show party members' pets is enabled
		if (VisibilityPlugin.Instance.Configuration.CurrentConfig.ShowPartyPet &&
		    this.containerManager.IsInContainer(UnitType.Players, ContainerType.Party,
			    characterPtr->GameObject.OwnerId)) return true;

		// Check if pet's owner is whitelisted
		return this.voidListManager.IsObjectWhitelisted(characterPtr->GameObject.OwnerId);
	}

	/// <summary>
	/// Process an Earthly Star (special pet type)
	/// </summary>
	public unsafe void ProcessEarthlyStar(Character* characterPtr, Character* localPlayer)
	{
		if (VisibilityPlugin.Instance.Configuration is { Enabled: true, HideStar: true }
		    && Service.Condition[ConditionFlag.InCombat]
		    && characterPtr->GameObject.OwnerId != localPlayer->GameObject.EntityId
		    && !this.containerManager.IsInContainer(UnitType.Players, ContainerType.Party,
			    characterPtr->GameObject.OwnerId))
			this.visibilityManager.HideGameObject(characterPtr);
	}
}
