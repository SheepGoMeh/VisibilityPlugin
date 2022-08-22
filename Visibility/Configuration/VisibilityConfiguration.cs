using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Configuration;
using Lumina.Excel.GeneratedSheets;
using Visibility.Utils;
using Visibility.Void;

namespace Visibility.Configuration
{
	public partial class VisibilityConfiguration : IPluginConfiguration
	{
		public int Version { get; set; }

		public Localization.Language Language { get; set; }

		public bool Enabled;
		public bool HideStar;
		public bool AdvancedEnabled;
		public bool EnableContextMenu;

		public List<VoidItem> VoidList { get; } = new List<VoidItem>();
		public List<VoidItem> Whitelist { get; } = new List<VoidItem>();

		[NonSerialized]
		private readonly bool[] showListWindow = { false, false };

		[NonSerialized]
		private readonly byte[][] buffer =
		{
			new byte[16],
			new byte[16],
			new byte[128],
			new byte[128],
			new byte[16],
			new byte[16],
			new byte[128],
			new byte[128],
			new byte[128]
		};

		[NonSerialized]
		public readonly Dictionary<string, Action<bool, bool, bool>> SettingDictionary = new(StringComparer.InvariantCultureIgnoreCase);

		[NonSerialized]
		public readonly HashSet<ushort> TerritoryTypeWhitelist = new HashSet<ushort>
		{
			792, // The Fall of Belah'dia 
			899, // The Falling City of Nym
			900, // The Endeavor
			939, // The Diadem
		};

		[NonSerialized] private readonly HashSet<ushort> allowedTerritory = new();

		[NonSerialized]
		private readonly Dictionary<ushort, string> territoryPlaceNameDictionary = new()
		{
			{ 0, "Default" }
		};

		public readonly Dictionary<ushort, TerritoryConfig> TerritoryConfigDictionary = new();

		[NonSerialized] public TerritoryConfig CurrentConfig = null!;
		[NonSerialized] private TerritoryConfig currentEditedConfig = null!;

		public void Init(ushort territoryType)
		{
			this.SettingDictionary[nameof(this.Enabled)] = (val, toggle, _) =>
			{
				this.Enabled.ToggleBool(val, toggle);
				
				if (!VisibilityPlugin.Instance.Disable) // Make sure the disable event is finished before enabling again
				{
					VisibilityPlugin.Instance.Disable = !this.Enabled;
				}
			};
			
			this.SettingDictionary[nameof(this.HideStar)] = (val, toggle, _) => this.HideStar.ToggleBool(val, toggle);
			this.SettingDictionary[nameof(this.AdvancedEnabled)] = (val, toggle, _) => this.AdvancedEnabled.ToggleBool(val, toggle);

			this.SettingDictionary[nameof(this.EnableContextMenu)] = (val, toggle, _) =>
			{
				this.EnableContextMenu.ToggleBool(val, toggle);

				VisibilityPlugin.Instance.ContextMenu.Toggle(val, toggle);
			};

			this.SettingDictionary[nameof(TerritoryConfig.HidePet)] = (val, toggle, edit) =>
			{
				if (edit)
				{
					this.currentEditedConfig.HidePet.ToggleBool(val, toggle);
				}
				else
				{
					this.CurrentConfig.HidePet.ToggleBool(val, toggle);
				}
				
				VisibilityPlugin.Instance.ShowPets(ContainerType.All);
			};

			this.SettingDictionary[nameof(TerritoryConfig.HidePlayer)] = (val, toggle, edit) =>
			{
				if (edit)
				{
					this.currentEditedConfig.HidePlayer.ToggleBool(val, toggle);
				}
				else
				{
					this.CurrentConfig.HidePlayer.ToggleBool(val, toggle);
				}
				
				VisibilityPlugin.Instance.ShowPlayers(ContainerType.All);
			};
			
			this.SettingDictionary[nameof(TerritoryConfig.HideChocobo)] = (val, toggle, edit) =>
			{
				if (edit)
				{
					this.currentEditedConfig.HideChocobo.ToggleBool(val, toggle);
				}
				else
				{
					this.CurrentConfig.HideChocobo.ToggleBool(val, toggle);
				}
				
				VisibilityPlugin.Instance.ShowChocobos(ContainerType.All);
			};
			
			this.SettingDictionary[nameof(TerritoryConfig.HideMinion)] = (val, toggle, edit) =>
			{
				if (edit)
				{
					this.currentEditedConfig.HideMinion.ToggleBool(val, toggle);
				}
				else
				{
					this.CurrentConfig.HideMinion.ToggleBool(val, toggle);
				}
				
				VisibilityPlugin.Instance.ShowMinions(ContainerType.All);
			};
			
			this.SettingDictionary[nameof(TerritoryConfig.ShowCompanyPet)] = (val, toggle, edit) =>
			{
				if (edit)
				{
					this.currentEditedConfig.ShowCompanyPet.ToggleBool(val, toggle);
				}
				else
				{
					this.CurrentConfig.ShowCompanyPet.ToggleBool(val, toggle);
				}
				
				VisibilityPlugin.Instance.ShowPets(ContainerType.Company);
			};
			
			this.SettingDictionary[nameof(TerritoryConfig.ShowCompanyPlayer)] = (val, toggle, edit) =>
			{
				if (edit)
				{
					this.currentEditedConfig.ShowCompanyPlayer.ToggleBool(val, toggle);
				}
				else
				{
					this.CurrentConfig.ShowCompanyPlayer.ToggleBool(val, toggle);
				}
				
				VisibilityPlugin.Instance.ShowPlayers(ContainerType.Company);
			};
			
			this.SettingDictionary[nameof(TerritoryConfig.ShowCompanyChocobo)] = (val, toggle, edit) =>
			{
				if (edit)
				{
					this.currentEditedConfig.ShowCompanyChocobo.ToggleBool(val, toggle);
				}
				else
				{
					this.CurrentConfig.ShowCompanyChocobo.ToggleBool(val, toggle);
				}
				
				VisibilityPlugin.Instance.ShowChocobos(ContainerType.Company);
			};

			this.SettingDictionary[nameof(TerritoryConfig.ShowCompanyMinion)] = (val, toggle, edit) =>
			{
				if (edit)
				{
					this.currentEditedConfig.ShowCompanyMinion.ToggleBool(val, toggle);
				}
				else
				{
					this.CurrentConfig.ShowCompanyMinion.ToggleBool(val, toggle);
				}

				VisibilityPlugin.Instance.ShowMinions(ContainerType.Company);
			};

			this.SettingDictionary[nameof(TerritoryConfig.ShowPartyPet)] = (val, toggle, edit) =>
			{
				if (edit)
				{
					this.currentEditedConfig.ShowPartyPet.ToggleBool(val, toggle);
				}
				else
				{
					this.CurrentConfig.ShowPartyPet.ToggleBool(val, toggle);
				}
				
				VisibilityPlugin.Instance.ShowPets(ContainerType.Party);
			};
			
			this.SettingDictionary[nameof(TerritoryConfig.ShowPartyPlayer)] = (val, toggle, edit) =>
			{
				if (edit)
				{
					this.currentEditedConfig.ShowPartyPlayer.ToggleBool(val, toggle);
				}
				else
				{
					this.CurrentConfig.ShowPartyPlayer.ToggleBool(val, toggle);
				}
				
				VisibilityPlugin.Instance.ShowPlayers(ContainerType.Party);
			};
			
			this.SettingDictionary[nameof(TerritoryConfig.ShowPartyChocobo)] = (val, toggle, edit) =>
			{
				if (edit)
				{
					this.currentEditedConfig.ShowPartyChocobo.ToggleBool(val, toggle);
				}
				else
				{
					this.CurrentConfig.ShowPartyChocobo.ToggleBool(val, toggle);
				}
				
				VisibilityPlugin.Instance.ShowChocobos(ContainerType.Party);
			};

			this.SettingDictionary[nameof(TerritoryConfig.ShowPartyMinion)] = (val, toggle, edit) =>
			{
				if (edit)
				{
					this.currentEditedConfig.ShowPartyMinion.ToggleBool(val, toggle);
				}
				else
				{
					this.CurrentConfig.ShowPartyMinion.ToggleBool(val, toggle);
				}

				VisibilityPlugin.Instance.ShowMinions(ContainerType.Party);
			};

			this.SettingDictionary[nameof(TerritoryConfig.ShowFriendPet)] = (val, toggle, edit) =>
			{
				if (edit)
				{
					this.currentEditedConfig.ShowFriendPet.ToggleBool(val, toggle);
				}
				else
				{
					this.CurrentConfig.ShowFriendPet.ToggleBool(val, toggle);
				}
				
				VisibilityPlugin.Instance.ShowPets(ContainerType.Friend);
			};
			
			this.SettingDictionary[nameof(TerritoryConfig.ShowFriendPlayer)] = (val, toggle, edit) =>
			{
				if (edit)
				{
					this.currentEditedConfig.ShowFriendPlayer.ToggleBool(val, toggle);
				}
				else
				{
					this.CurrentConfig.ShowFriendPlayer.ToggleBool(val, toggle);
				}
				
				VisibilityPlugin.Instance.ShowPlayers(ContainerType.Friend);
			};
			
			this.SettingDictionary[nameof(TerritoryConfig.ShowFriendChocobo)] = (val, toggle, edit) =>
			{
				if (edit)
				{
					this.currentEditedConfig.ShowFriendChocobo.ToggleBool(val, toggle);
				}
				else
				{
					this.CurrentConfig.ShowFriendChocobo.ToggleBool(val, toggle);
				}
				
				VisibilityPlugin.Instance.ShowChocobos(ContainerType.Friend);
			};

			this.SettingDictionary[nameof(TerritoryConfig.ShowFriendMinion)] = (val, toggle, edit) =>
			{
				if (edit)
				{
					this.currentEditedConfig.ShowFriendMinion.ToggleBool(val, toggle);
				}
				else
				{
					this.CurrentConfig.ShowFriendMinion.ToggleBool(val, toggle);
				}

				VisibilityPlugin.Instance.ShowMinions(ContainerType.Friend);
			};

			this.SettingDictionary[nameof(TerritoryConfig.ShowDeadPlayer)] = (val, toggle, edit) =>
			{
				if (edit)
				{
					this.currentEditedConfig.ShowDeadPlayer.ToggleBool(val, toggle);
				}
				else
				{
					this.CurrentConfig.ShowDeadPlayer.ToggleBool(val, toggle);
				}
			};

			var valueTuples = VisibilityPlugin.DataManager.GameData.Excel.GetSheet<TerritoryType>()!.Where(
				x => (x.TerritoryIntendedUse is 0 or 1 or 13 or 19 or 23 ||
				      this.TerritoryTypeWhitelist.Contains((ushort)x.RowId)) && !string.IsNullOrEmpty(x.Name) &&
				     x.RowId != 136).Select(x => ((ushort)x.RowId, x.PlaceName?.Value?.Name ?? "Unknown Place"));

			foreach (var (rowId, placeName) in valueTuples)
			{
				this.allowedTerritory.Add(rowId);
				this.territoryPlaceNameDictionary[rowId] = placeName;
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
				this.currentEditedConfig = this.TerritoryConfigDictionary[territoryType];
				this.currentEditedConfig.TerritoryType = territoryType;
			}
			else
			{
				this.CurrentConfig = this.TerritoryConfigDictionary[territoryType];
				this.CurrentConfig.TerritoryType = territoryType;
				this.currentEditedConfig = this.CurrentConfig;
			}
		}

		public void Save()
		{
			VisibilityPlugin.PluginInterface.SavePluginConfig(this);
		}
	}
}
