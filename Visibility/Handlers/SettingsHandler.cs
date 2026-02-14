using System;
using System.Collections.Generic;

using Visibility.Configuration;
using Visibility.Utils;

namespace Visibility.Handlers;

public class SettingsHandler
{
	private readonly Dictionary<string, Action<bool, bool, bool>> settingActions =
		new(StringComparer.InvariantCultureIgnoreCase);

	private readonly VisibilityConfiguration configurationInstance;
	private readonly FrameworkHandler frameworkHandler;
	private readonly FrameworkUpdateHandler frameworkUpdateHandler;

	public SettingsHandler(
		VisibilityConfiguration configurationInstance,
		FrameworkHandler frameworkHandler,
		FrameworkUpdateHandler frameworkUpdateHandler)
	{
		this.configurationInstance = configurationInstance;
		this.frameworkHandler = frameworkHandler;
		this.frameworkUpdateHandler = frameworkUpdateHandler;
		this.InitializeSettingActions();
	}

	/// <summary>
	/// Checks if a setting action exists for the given key (property name).
	/// </summary>
	/// <param name="key">The property name.</param>
	/// <returns>True if an action exists, false otherwise.</returns>
	public bool ContainsKey(string key) => this.settingActions.ContainsKey(key);

	/// <summary>
	/// Gets a read-only collection of all setting keys.
	/// </summary>
	/// <returns>A read-only collection of all setting keys.</returns>
	public IEnumerable<string> GetKeys() => this.settingActions.Keys;

	/// <summary>
	/// Gets a setting action associated with the given key.
	/// </summary>
	/// <param name="key">The property name.</param>
	/// <returns>The action associated with the given key, or null if not found.</returns>
	public Action<bool, bool, bool>? GetAction(string key) => this.settingActions.GetValueOrDefault(key);

	/// <summary>
	/// Invokes the setting action associated with the given key.
	/// </summary>
	/// <param name="key">The property name.</param>
	/// <param name="val">The value parameter for the action.</param>
	/// <param name="toggle">The toggle parameter for the action.</param>
	/// <param name="edit">The edit parameter for the action (used for territory configs).</param>
	public void Invoke(string key, bool val, bool toggle, bool edit)
	{
		if (!this.settingActions.TryGetValue(key, out Action<bool, bool, bool>? action))
			return;

		action.Invoke(val, toggle, edit);
	}

	/// <summary>
	/// Creates a generic action for handling territory-specific boolean settings.
	/// </summary>
	private Action<bool, bool, bool> CreateToggleAction(
		Action<TerritoryConfig, bool, bool> propertyToggler,
		Action? afterToggleAction = null)
	{
		return (val, toggle, edit) =>
		{
			TerritoryConfig configToModify =
				edit ? this.configurationInstance.CurrentEditedConfig : this.configurationInstance.CurrentConfig;
			propertyToggler(configToModify, val, toggle);
			afterToggleAction?.Invoke();
		};
	}

	/// <summary>
	/// Creates a generic action for handling direct VisibilityConfiguration boolean settings.
	/// </summary>
	private Action<bool, bool, bool> CreateDirectToggleAction(
		Action<bool, bool> directPropertyToggler,
		Action? afterToggleAction = null)
	{
		return (val, toggle, _) => // Ignores edit flag
		{
			directPropertyToggler(val, toggle);
			afterToggleAction?.Invoke();
		};
	}

	/// <summary>
	/// Populates the _settingActions dictionary.
	/// </summary>
	private void InitializeSettingActions()
	{
		// --- Direct Settings ---
		this.settingActions[nameof(this.configurationInstance.Enabled)] = this.CreateDirectToggleAction(
			(v, t) => this.configurationInstance.Enabled.ToggleBool(v, t),
			() =>
			{
				if (!this.frameworkUpdateHandler.Disable)
					this.frameworkUpdateHandler.Disable = !this.configurationInstance.Enabled;
			}
		);

		this.settingActions[nameof(this.configurationInstance.HideStar)] =
			this.CreateDirectToggleAction((v, t) => this.configurationInstance.HideStar.ToggleBool(v, t)
			);

		this.settingActions[nameof(this.configurationInstance.AdvancedEnabled)] =
			this.CreateDirectToggleAction((v, t) =>
				this.configurationInstance.AdvancedEnabled.ToggleBool(v, t),
				() =>
				{
					this.configurationInstance.UpdateCurrentConfig(Service.ClientState.TerritoryType);
				}
			);

		this.settingActions[nameof(this.configurationInstance.EnableContextMenu)] =
			this.CreateDirectToggleAction((v, t) =>
					this.configurationInstance.EnableContextMenu.ToggleBool(v, t)
				// afterToggleAction: () => { /* TODO: Switch to dalamud service */ }
			);

		this.settingActions[nameof(this.configurationInstance.ShowTargetOfTarget)] =
			this.CreateDirectToggleAction((v, t) =>
				this.configurationInstance.ShowTargetOfTarget.ToggleBool(v, t)
			);

		// --- Territory Config Settings (using nameof(TerritoryConfig.Property)) ---

		// Hide Section
		this.settingActions[nameof(TerritoryConfig.HidePet)] = this.CreateToggleAction(
			(config, v, t) => config.HidePet.ToggleBool(v, t),
			() => this.frameworkHandler.ShowPets(ContainerType.All)
		);

		this.settingActions[nameof(TerritoryConfig.HidePlayer)] = this.CreateToggleAction(
			(config, v, t) => config.HidePlayer.ToggleBool(v, t),
			() => this.frameworkHandler.ShowPlayers(ContainerType.All)
		);

		this.settingActions[nameof(TerritoryConfig.HideChocobo)] = this.CreateToggleAction(
			(config, v, t) => config.HideChocobo.ToggleBool(v, t),
			() => this.frameworkHandler.ShowChocobos(ContainerType.All)
		);

		this.settingActions[nameof(TerritoryConfig.HideMinion)] = this.CreateToggleAction(
			(config, v, t) => config.HideMinion.ToggleBool(v, t),
			() => this.frameworkHandler.ShowMinions(ContainerType.All)
		);

		this.settingActions[nameof(TerritoryConfig.HidePetInCombat)] = this.CreateToggleAction(
			(config, v, t) => config.HidePetInCombat.ToggleBool(v, t),
			() => this.frameworkHandler.ShowPets(ContainerType.All)
		);

		this.settingActions[nameof(TerritoryConfig.HidePlayerInCombat)] = this.CreateToggleAction(
			(config, v, t) => config.HidePlayerInCombat.ToggleBool(v, t),
			() => this.frameworkHandler.ShowPlayers(ContainerType.All)
		);

		this.settingActions[nameof(TerritoryConfig.HideChocoboInCombat)] = this.CreateToggleAction(
			(config, v, t) => config.HideChocoboInCombat.ToggleBool(v, t),
			() => this.frameworkHandler.ShowChocobos(ContainerType.All)
		);

		this.settingActions[nameof(TerritoryConfig.HideMinionInCombat)] = this.CreateToggleAction(
			(config, v, t) => config.HideMinionInCombat.ToggleBool(v, t),
			() => this.frameworkHandler.ShowMinions(ContainerType.All)
		);

		// Show Company Section
		this.settingActions[nameof(TerritoryConfig.ShowCompanyPet)] = this.CreateToggleAction(
			(config, v, t) => config.ShowCompanyPet.ToggleBool(v, t),
			() => this.frameworkHandler.ShowPets(ContainerType.Company)
		);

		this.settingActions[nameof(TerritoryConfig.ShowCompanyPlayer)] = this.CreateToggleAction(
			(config, v, t) => config.ShowCompanyPlayer.ToggleBool(v, t),
			() => this.frameworkHandler.ShowPlayers(ContainerType.Company)
		);

		this.settingActions[nameof(TerritoryConfig.ShowCompanyChocobo)] = this.CreateToggleAction(
			(config, v, t) => config.ShowCompanyChocobo.ToggleBool(v, t),
			() => this.frameworkHandler.ShowChocobos(ContainerType.Company)
		);

		this.settingActions[nameof(TerritoryConfig.ShowCompanyMinion)] = this.CreateToggleAction(
			(config, v, t) => config.ShowCompanyMinion.ToggleBool(v, t),
			() => this.frameworkHandler.ShowMinions(ContainerType.Company)
		);

		// Show Party Section
		this.settingActions[nameof(TerritoryConfig.ShowPartyPet)] = this.CreateToggleAction(
			(config, v, t) => config.ShowPartyPet.ToggleBool(v, t),
			() => this.frameworkHandler.ShowPets(ContainerType.Party)
		);

		this.settingActions[nameof(TerritoryConfig.ShowPartyPlayer)] = this.CreateToggleAction(
			(config, v, t) => config.ShowPartyPlayer.ToggleBool(v, t),
			() => this.frameworkHandler.ShowPlayers(ContainerType.Party)
		);

		this.settingActions[nameof(TerritoryConfig.ShowPartyChocobo)] = this.CreateToggleAction(
			(config, v, t) => config.ShowPartyChocobo.ToggleBool(v, t),
			() => this.frameworkHandler.ShowChocobos(ContainerType.Party)
		);

		this.settingActions[nameof(TerritoryConfig.ShowPartyMinion)] = this.CreateToggleAction(
			(config, v, t) => config.ShowPartyMinion.ToggleBool(v, t),
			() => this.frameworkHandler.ShowMinions(ContainerType.Party)
		);

		// Show Friend Section
		this.settingActions[nameof(TerritoryConfig.ShowFriendPet)] = this.CreateToggleAction(
			(config, v, t) => config.ShowFriendPet.ToggleBool(v, t),
			() => this.frameworkHandler.ShowPets(ContainerType.Friend)
		);

		this.settingActions[nameof(TerritoryConfig.ShowFriendPlayer)] = this.CreateToggleAction(
			(config, v, t) => config.ShowFriendPlayer.ToggleBool(v, t),
			() => this.frameworkHandler.ShowPlayers(ContainerType.Friend)
		);

		this.settingActions[nameof(TerritoryConfig.ShowFriendChocobo)] = this.CreateToggleAction(
			(config, v, t) => config.ShowFriendChocobo.ToggleBool(v, t),
			() => this.frameworkHandler.ShowChocobos(ContainerType.Friend)
		);

		this.settingActions[nameof(TerritoryConfig.ShowFriendMinion)] = this.CreateToggleAction(
			(config, v, t) => config.ShowFriendMinion.ToggleBool(v, t),
			() => this.frameworkHandler.ShowMinions(ContainerType.Friend)
		);

		// Special Case: ShowDeadPlayer (no afterToggleAction)
		this.settingActions[nameof(TerritoryConfig.ShowDeadPlayer)] = this.CreateToggleAction((config, v, t) =>
				config.ShowDeadPlayer.ToggleBool(v, t)
			// No specific afterToggleAction needed
		);

		this.settingActions[nameof(this.configurationInstance.CrowdCullEnabled)] =
			this.CreateDirectToggleAction((v, t) =>
					this.configurationInstance.CrowdCullEnabled.ToggleBool(v, t),
				() => this.frameworkHandler.ShowPlayers(ContainerType.All)
			);

		this.settingActions[nameof(this.configurationInstance.CrowdCullCountInside)] =
			this.CreateDirectToggleAction((v, t) =>
					this.configurationInstance.CrowdCullCountInside.ToggleBool(v, t),
				() => this.frameworkHandler.ShowPlayers(ContainerType.All)
			);
	}
}
