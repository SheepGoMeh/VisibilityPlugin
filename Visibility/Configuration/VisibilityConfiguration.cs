using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Dalamud.Configuration;
using Lumina.Excel.GeneratedSheets;
using Visibility.Utils;
using Visibility.Void;

namespace Visibility.Configuration
{
	public partial class VisibilityConfiguration : IPluginConfiguration
	{
		public int Version { get; set; }

		public Localization.Language Language { get; private set; }

		public bool Enabled;
		public bool AdvancedEnabled;

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
		public readonly Dictionary<string, Action<int>> SettingDictionary = new Dictionary<string, Action<int>>();

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

		private void ChangeSetting(string propertyName)
		{
			switch (propertyName)
			{
				case "this.Enabled":
					if (!VisibilityPlugin.Instance.Disable) // Make sure the disable event is finished before enabling again
					{
						VisibilityPlugin.Instance.Disable = !this.Enabled;
					}
					break;
				case "this.CurrentConfig.HidePet":
					VisibilityPlugin.Instance.ShowPets(ContainerType.All);
					break;
				case "this.CurrentConfig.HidePlayer":
					VisibilityPlugin.Instance.ShowPlayers(ContainerType.All);
					break;
				case "this.CurrentConfig.HideMinion":
					VisibilityPlugin.Instance.ShowMinions(ContainerType.All);
					break;
				case "this.CurrentConfig.HideChocobo":
					VisibilityPlugin.Instance.ShowChocobos(ContainerType.All);
					break;
				case "this.CurrentConfig.ShowCompanyPet":
					VisibilityPlugin.Instance.ShowPets(ContainerType.Company);
					break;
				case "this.CurrentConfig.ShowCompanyPlayer":
					VisibilityPlugin.Instance.ShowPlayers(ContainerType.Company);
					break;
				case "this.CurrentConfig.ShowCompanyMinion":
					VisibilityPlugin.Instance.ShowMinions(ContainerType.Company);
					break;
				case "this.CurrentConfig.ShowCompanyChocobo":
					VisibilityPlugin.Instance.ShowChocobos(ContainerType.Company);
					break;
				case "this.CurrentConfig.ShowPartyPet":
					VisibilityPlugin.Instance.ShowPets(ContainerType.Party);
					break;
				case "this.CurrentConfig.ShowPartyPlayer":
					VisibilityPlugin.Instance.ShowPlayers(ContainerType.Party);
					break;
				case "this.CurrentConfig.ShowPartyMinion":
					VisibilityPlugin.Instance.ShowMinions(ContainerType.Party);
					break;
				case "this.CurrentConfig.ShowPartyChocobo":
					VisibilityPlugin.Instance.ShowChocobos(ContainerType.Party);
					break;
				case "this.CurrentConfig.ShowFriendPet":
					VisibilityPlugin.Instance.ShowPets(ContainerType.Friend);
					break;
				case "this.CurrentConfig.ShowFriendPlayer":
					VisibilityPlugin.Instance.ShowPlayers(ContainerType.Friend);
					break;
				case "this.CurrentConfig.ShowFriendMinion":
					VisibilityPlugin.Instance.ShowMinions(ContainerType.Friend);
					break;
				case "this.CurrentConfig.ShowFriendChocobo":
					VisibilityPlugin.Instance.ShowChocobos(ContainerType.Friend);
					break;
			}
		}

		private void ChangeSetting(
			ref bool property,
			int val,
			bool edit = false,
			[CallerArgumentExpression("property")] string propertyName = "")
		{
			property = val > 1 ? !property : val > 0;
			
			if (propertyName.Contains("currentEditedConfig"))
			{
				if (edit)
				{
					return;
				}

				propertyName = propertyName.Replace("currentEditedConfig", "CurrentConfig");
			}

			this.ChangeSetting(propertyName);
		}

		public void Init(ushort territoryType)
		{
			this.SettingDictionary["enabled"] = x => this.ChangeSetting(ref this.Enabled, x);
			this.SettingDictionary["hidepet"] = x => this.ChangeSetting(ref this.CurrentConfig.HidePet, x);
			this.SettingDictionary["hidestar"] = x => this.ChangeSetting(ref this.CurrentConfig.HideStar, x);
			this.SettingDictionary["hideplayer"] = x => this.ChangeSetting(ref this.CurrentConfig.HidePlayer, x);
			this.SettingDictionary["hidechocobo"] = x => this.ChangeSetting(ref this.CurrentConfig.HideChocobo, x);
			this.SettingDictionary["hideminion"] = x => this.ChangeSetting(ref this.CurrentConfig.HideMinion, x);
			this.SettingDictionary["showcompanypet"] = x => this.ChangeSetting(ref this.CurrentConfig.ShowCompanyPet, x);
			this.SettingDictionary["showcompanyplayer"] = x => this.ChangeSetting(ref this.CurrentConfig.ShowCompanyPlayer, x);
			this.SettingDictionary["showcompanychocobo"] = x => this.ChangeSetting(ref this.CurrentConfig.ShowCompanyChocobo, x);
			this.SettingDictionary["showcompanyminion"] = x => this.ChangeSetting(ref this.CurrentConfig.ShowCompanyMinion, x);
			this.SettingDictionary["showpartypet"] = x => this.ChangeSetting(ref this.CurrentConfig.ShowPartyPet, x);
			this.SettingDictionary["showpartyplayer"] = x => this.ChangeSetting(ref this.CurrentConfig.ShowPartyPlayer, x);
			this.SettingDictionary["showpartychocobo"] = x => this.ChangeSetting(ref this.CurrentConfig.ShowPartyChocobo, x);
			this.SettingDictionary["showpartyminion"] = x => this.ChangeSetting(ref this.CurrentConfig.ShowPartyMinion, x);
			this.SettingDictionary["showfriendpet"] = x => this.ChangeSetting(ref this.CurrentConfig.ShowFriendPet, x);
			this.SettingDictionary["showfriendplayer"] = x => this.ChangeSetting(ref this.CurrentConfig.ShowFriendPlayer, x);
			this.SettingDictionary["showfriendchocobo"] = x => this.ChangeSetting(ref this.CurrentConfig.ShowFriendChocobo, x);
			this.SettingDictionary["showfriendminion"] = x => this.ChangeSetting(ref this.CurrentConfig.ShowFriendMinion, x);
			this.SettingDictionary["showdeadplayer"] = x => this.ChangeSetting(ref this.CurrentConfig.ShowDeadPlayer, x);

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
