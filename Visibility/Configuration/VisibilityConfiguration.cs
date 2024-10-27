﻿using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Configuration;

using Lumina.Excel.GeneratedSheets;

using Visibility.Void;

namespace Visibility.Configuration;

public class VisibilityConfiguration: IPluginConfiguration
{
	public int Version { get; set; }

	public Localization.Language Language { get; set; }

	public bool Enabled;
	public bool HideStar;
	public bool AdvancedEnabled;
	public bool EnableContextMenu;
	public bool ShowTargetOfTarget;

	public List<VoidItem> VoidList { get; } = [];

	public List<VoidItem> Whitelist { get; } = [];

	[NonSerialized] public Dictionary<ulong, VoidItem> VoidDictionary = null!;
	[NonSerialized] public Dictionary<ulong, VoidItem> WhitelistDictionary = null!;

	[NonSerialized] public readonly Dictionary<string, Action<bool, bool, bool>> SettingDictionary =
		new(StringComparer.InvariantCultureIgnoreCase);

	[NonSerialized] public readonly HashSet<ushort> TerritoryTypeWhitelist = [];

	[NonSerialized] private readonly HashSet<ushort> allowedTerritory = [];

	[NonSerialized]
	public readonly Dictionary<ushort, string> TerritoryPlaceNameDictionary = new() { { 0, "Default" } };

	public readonly Dictionary<ushort, TerritoryConfig> TerritoryConfigDictionary = new();

	[NonSerialized] public TerritoryConfig CurrentConfig = null!;
	[NonSerialized] public TerritoryConfig CurrentEditedConfig = null!;

	public void Init(ushort territoryType)
	{
		this.VoidDictionary = this.VoidList.Where(x => x.Id != 0).ToDictionary(x => x.Id, x => x);
		this.WhitelistDictionary = this.Whitelist.Where(x => x.Id != 0).ToDictionary(x => x.Id, x => x);

		this.SettingDictionary[nameof(this.Enabled)] = (val, toggle, _) =>
		{
			this.Enabled.ToggleBool(val, toggle);

			if (!VisibilityPlugin.Instance.Disable) // Make sure the disable event is finished before enabling again
			{
				VisibilityPlugin.Instance.Disable = !this.Enabled;
			}
		};

		this.SettingDictionary[nameof(this.HideStar)] = (val, toggle, _) => this.HideStar.ToggleBool(val, toggle);
		this.SettingDictionary[nameof(this.AdvancedEnabled)] =
			(val, toggle, _) => this.AdvancedEnabled.ToggleBool(val, toggle);

		this.SettingDictionary[nameof(this.EnableContextMenu)] = (val, toggle, _) =>
		{
			this.EnableContextMenu.ToggleBool(val, toggle);

			// TODO: Switch to dalamud service
			// VisibilityPlugin.Instance.ContextMenu.Toggle(val, toggle);
		};

		this.SettingDictionary[nameof(this.ShowTargetOfTarget)] = (val, toggle, _) => this.ShowTargetOfTarget.ToggleBool(val, toggle);

		this.SettingDictionary[nameof(TerritoryConfig.HidePet)] = (val, toggle, edit) =>
		{
			if (edit)
			{
				this.CurrentEditedConfig.HidePet.ToggleBool(val, toggle);
			}
			else
			{
				this.CurrentConfig.HidePet.ToggleBool(val, toggle);
			}

			VisibilityPlugin.Instance.ShowAllPets();
		};

		this.SettingDictionary[nameof(TerritoryConfig.HidePlayer)] = (val, toggle, edit) =>
		{
			if (edit)
			{
				this.CurrentEditedConfig.HidePlayer.ToggleBool(val, toggle);
			}
			else
			{
				this.CurrentConfig.HidePlayer.ToggleBool(val, toggle);
			}

			VisibilityPlugin.Instance.ShowAllPlayers();
		};

		this.SettingDictionary[nameof(TerritoryConfig.HideChocobo)] = (val, toggle, edit) =>
		{
			if (edit)
			{
				this.CurrentEditedConfig.HideChocobo.ToggleBool(val, toggle);
			}
			else
			{
				this.CurrentConfig.HideChocobo.ToggleBool(val, toggle);
			}

			VisibilityPlugin.Instance.ShowAllChocobos();
		};

		this.SettingDictionary[nameof(TerritoryConfig.HideMinion)] = (val, toggle, edit) =>
		{
			if (edit)
			{
				this.CurrentEditedConfig.HideMinion.ToggleBool(val, toggle);
			}
			else
			{
				this.CurrentConfig.HideMinion.ToggleBool(val, toggle);
			}

			VisibilityPlugin.Instance.ShowAllCompanions();
		};

		this.SettingDictionary[nameof(TerritoryConfig.ShowCompanyPet)] = (val, toggle, edit) =>
		{
			if (edit)
			{
				this.CurrentEditedConfig.ShowCompanyPet.ToggleBool(val, toggle);
			}
			else
			{
				this.CurrentConfig.ShowCompanyPet.ToggleBool(val, toggle);
			}

			VisibilityPlugin.Instance.ShowCompanyPets();
		};

		this.SettingDictionary[nameof(TerritoryConfig.ShowCompanyPlayer)] = (val, toggle, edit) =>
		{
			if (edit)
			{
				this.CurrentEditedConfig.ShowCompanyPlayer.ToggleBool(val, toggle);
			}
			else
			{
				this.CurrentConfig.ShowCompanyPlayer.ToggleBool(val, toggle);
			}

			VisibilityPlugin.Instance.ShowCompanyPlayers();
		};

		this.SettingDictionary[nameof(TerritoryConfig.ShowCompanyChocobo)] = (val, toggle, edit) =>
		{
			if (edit)
			{
				this.CurrentEditedConfig.ShowCompanyChocobo.ToggleBool(val, toggle);
			}
			else
			{
				this.CurrentConfig.ShowCompanyChocobo.ToggleBool(val, toggle);
			}

			VisibilityPlugin.Instance.ShowCompanyChocobos();
		};

		this.SettingDictionary[nameof(TerritoryConfig.ShowCompanyMinion)] = (val, toggle, edit) =>
		{
			if (edit)
			{
				this.CurrentEditedConfig.ShowCompanyMinion.ToggleBool(val, toggle);
			}
			else
			{
				this.CurrentConfig.ShowCompanyMinion.ToggleBool(val, toggle);
			}

			VisibilityPlugin.Instance.ShowCompanyCompanions();
		};

		this.SettingDictionary[nameof(TerritoryConfig.ShowPartyPet)] = (val, toggle, edit) =>
		{
			if (edit)
			{
				this.CurrentEditedConfig.ShowPartyPet.ToggleBool(val, toggle);
			}
			else
			{
				this.CurrentConfig.ShowPartyPet.ToggleBool(val, toggle);
			}

			VisibilityPlugin.Instance.ShowPartyPets();
		};

		this.SettingDictionary[nameof(TerritoryConfig.ShowPartyPlayer)] = (val, toggle, edit) =>
		{
			if (edit)
			{
				this.CurrentEditedConfig.ShowPartyPlayer.ToggleBool(val, toggle);
			}
			else
			{
				this.CurrentConfig.ShowPartyPlayer.ToggleBool(val, toggle);
			}

			VisibilityPlugin.Instance.ShowPartyPlayers();
		};

		this.SettingDictionary[nameof(TerritoryConfig.ShowPartyChocobo)] = (val, toggle, edit) =>
		{
			if (edit)
			{
				this.CurrentEditedConfig.ShowPartyChocobo.ToggleBool(val, toggle);
			}
			else
			{
				this.CurrentConfig.ShowPartyChocobo.ToggleBool(val, toggle);
			}

			VisibilityPlugin.Instance.ShowPartyChocobos();
		};

		this.SettingDictionary[nameof(TerritoryConfig.ShowPartyMinion)] = (val, toggle, edit) =>
		{
			if (edit)
			{
				this.CurrentEditedConfig.ShowPartyMinion.ToggleBool(val, toggle);
			}
			else
			{
				this.CurrentConfig.ShowPartyMinion.ToggleBool(val, toggle);
			}

			VisibilityPlugin.Instance.ShowPartyCompanions();
		};

		this.SettingDictionary[nameof(TerritoryConfig.ShowFriendPet)] = (val, toggle, edit) =>
		{
			if (edit)
			{
				this.CurrentEditedConfig.ShowFriendPet.ToggleBool(val, toggle);
			}
			else
			{
				this.CurrentConfig.ShowFriendPet.ToggleBool(val, toggle);
			}

			VisibilityPlugin.Instance.ShowFriendPets();
		};

		this.SettingDictionary[nameof(TerritoryConfig.ShowFriendPlayer)] = (val, toggle, edit) =>
		{
			if (edit)
			{
				this.CurrentEditedConfig.ShowFriendPlayer.ToggleBool(val, toggle);
			}
			else
			{
				this.CurrentConfig.ShowFriendPlayer.ToggleBool(val, toggle);
			}

			VisibilityPlugin.Instance.ShowFriendPlayers();
		};

		this.SettingDictionary[nameof(TerritoryConfig.ShowFriendChocobo)] = (val, toggle, edit) =>
		{
			if (edit)
			{
				this.CurrentEditedConfig.ShowFriendChocobo.ToggleBool(val, toggle);
			}
			else
			{
				this.CurrentConfig.ShowFriendChocobo.ToggleBool(val, toggle);
			}

			VisibilityPlugin.Instance.ShowFriendChocobos();
		};

		this.SettingDictionary[nameof(TerritoryConfig.ShowFriendMinion)] = (val, toggle, edit) =>
		{
			if (edit)
			{
				this.CurrentEditedConfig.ShowFriendMinion.ToggleBool(val, toggle);
			}
			else
			{
				this.CurrentConfig.ShowFriendMinion.ToggleBool(val, toggle);
			}

			VisibilityPlugin.Instance.ShowFriendCompanions();
		};

		this.SettingDictionary[nameof(TerritoryConfig.ShowDeadPlayer)] = (val, toggle, edit) =>
		{
			if (edit)
			{
				this.CurrentEditedConfig.ShowDeadPlayer.ToggleBool(val, toggle);
			}
			else
			{
				this.CurrentConfig.ShowDeadPlayer.ToggleBool(val, toggle);
			}
		};

		IEnumerable<(ushort, string)>? valueTuples = Service.DataManager.GameData.Excel.GetSheet<TerritoryType>()?
			.Where(
				x => (x.TerritoryIntendedUse is 0 or 1 or 13 or 19 or 21 or 23 or 44 or 46 or 47 ||
				      this.TerritoryTypeWhitelist.Contains((ushort)x.RowId)) && !string.IsNullOrEmpty(x.Name) &&
				     x.RowId != 136)
			.Select(x => ((ushort)x.RowId, x.PlaceName?.Value?.Name ?? "Unknown Place"));

		if (valueTuples != null)
		{
			foreach ((ushort rowId, string? placeName) in valueTuples)
			{
				this.allowedTerritory.Add(rowId);
				this.TerritoryTypeWhitelist.Add(rowId);
				this.TerritoryPlaceNameDictionary[rowId] = placeName;
			}
		}

		this.UpdateCurrentConfig(territoryType);
	}

	public void UpdateCurrentConfig(ushort territoryType, bool edit = false)
	{
		if (this.AdvancedEnabled == false ||
		    this.allowedTerritory.Contains(territoryType) == false)
		{
			territoryType = 0;
		}

		if (this.TerritoryConfigDictionary.ContainsKey(territoryType) == false)
		{
			this.TerritoryConfigDictionary[territoryType] = territoryType == 0
				? new TerritoryConfig()
				: this.TerritoryConfigDictionary[0].Clone();
		}

		if (edit)
		{
			this.CurrentEditedConfig = this.TerritoryConfigDictionary[territoryType];
			this.CurrentEditedConfig.TerritoryType = territoryType;
		}
		else
		{
			this.CurrentConfig = this.TerritoryConfigDictionary[territoryType];
			this.CurrentConfig.TerritoryType = territoryType;
			this.CurrentEditedConfig = this.CurrentConfig;
		}
	}

	public void Save() => Service.PluginInterface.SavePluginConfig(this);
}
