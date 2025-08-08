using Dalamud.Game.ClientState.Conditions;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Visibility.Configuration;

namespace Visibility.Utils.EntityHandlers;

/// <summary>
/// Handles visibility logic for minion entities
/// </summary>
public class MinionHandler
{
	private readonly ContainerManager containerManager;
	private readonly ObjectVisibilityManager visibilityManager;

	private static VisibilityConfiguration Configuration => VisibilityPlugin.Instance.Configuration;
	private static TerritoryConfig CurrentConfig => VisibilityPlugin.Instance.Configuration.CurrentConfig;

	public MinionHandler(
		ContainerManager containerManager,
		ObjectVisibilityManager visibilityManager)
	{
		this.containerManager = containerManager;
		this.visibilityManager = visibilityManager;
	}

	/// <summary>
	/// Process a minion entity and determine its visibility
	/// </summary>
	public unsafe void ProcessMinion(Character* characterPtr, Character* localPlayer)
	{
		if (localPlayer == null ||
		    characterPtr->CompanionOwnerId == localPlayer->GameObject.EntityId ||
		    this.visibilityManager.ShowGameObject(characterPtr, ObjectVisibilityManager.ObjectType.Companion))
			return;

		if (!Configuration.Enabled ||
		    !CurrentConfig.HideMinion)
			return;

		// Add to containers
		this.UpdateContainers(characterPtr);

		// Check visibility conditions
		if (this.ShouldShowMinion(characterPtr))
		{
			this.visibilityManager.MarkObjectToShow(characterPtr->GameObject.EntityId);
			return;
		}

		this.visibilityManager.HideGameObject(characterPtr, ObjectVisibilityManager.ObjectType.Companion);
	}

	/// <summary>
	/// Update container memberships for the minion
	/// </summary>
	private unsafe void UpdateContainers(Character* characterPtr)
	{
		// All minions container
		this.containerManager.AddToContainer(UnitType.Minions, ContainerType.All, characterPtr->CompanionOwnerId);

		// Friend's minion container
		if (this.containerManager.IsInContainer(UnitType.Players, ContainerType.Friend, characterPtr->CompanionOwnerId))
		{
			this.containerManager.AddToContainer(UnitType.Minions, ContainerType.Friend,
				characterPtr->CompanionOwnerId);
		}

		// Party member's minion container
		if (this.containerManager.IsInContainer(UnitType.Players, ContainerType.Party, characterPtr->CompanionOwnerId))
			this.containerManager.AddToContainer(UnitType.Minions, ContainerType.Party, characterPtr->CompanionOwnerId);

		// Company member's minion container
		if (this.containerManager.IsInContainer(UnitType.Players, ContainerType.Company,
			    characterPtr->CompanionOwnerId))
		{
			this.containerManager.AddToContainer(UnitType.Minions, ContainerType.Company,
				characterPtr->CompanionOwnerId);
		}
	}

	/// <summary>
	/// Determine if a minion should be shown based on configuration settings
	/// </summary>
	private unsafe bool ShouldShowMinion(Character* characterPtr)
	{
		// Check if minion's owner is a friend and show friends' minions is enabled
		if (CurrentConfig.ShowFriendMinion &&
		    this.containerManager.IsInContainer(UnitType.Players, ContainerType.Friend, characterPtr->CompanionOwnerId))
			return true;

		// Check if minion's owner is in the same company and show company members' minions is enabled
		if (CurrentConfig.ShowCompanyMinion &&
		    this.containerManager.IsInContainer(UnitType.Players, ContainerType.Company,
			    characterPtr->CompanionOwnerId)) return true;

		// Check if minion's owner is in the party and show party members' minions is enabled
		if (CurrentConfig.ShowPartyMinion &&
		    this.containerManager.IsInContainer(UnitType.Players, ContainerType.Party,
			    characterPtr->CompanionOwnerId))
			return true;

		// Check if local player is in combat and hide minions in combat is enabled
		return CurrentConfig is { HideMinionInCombat: true, HideMinion: false } &&
		       !Service.Condition[ConditionFlag.InCombat];
	}
}
