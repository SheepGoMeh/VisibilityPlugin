using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Configuration;

using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;

using Visibility.Handlers;
using Visibility.Utils;
using Visibility.Void;

namespace Visibility.Configuration;

public partial class VisibilityConfiguration: IPluginConfiguration
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

	[NonSerialized] public SettingsHandler SettingsHandler = null!;

	[NonSerialized] public readonly HashSet<ushort> TerritoryTypeWhitelist = [];

	[NonSerialized] private readonly HashSet<ushort> allowedTerritory = [];

	[NonSerialized]
	public readonly Dictionary<ushort, string> TerritoryPlaceNameDictionary = new() { { 0, "Default" } };

	public readonly Dictionary<ushort, TerritoryConfig> TerritoryConfigDictionary = new();

	[NonSerialized] public TerritoryConfig CurrentConfig = null!;
	[NonSerialized] public TerritoryConfig CurrentEditedConfig = null!;

	public void Init(ushort territoryType)
	{
		this.VoidDictionary = this.VoidList.Where(x => x.Id != 0).DistinctBy(x => x.Id).ToDictionary(x => x.Id, x => x);
		this.WhitelistDictionary = this.Whitelist.Where(x => x.Id != 0).DistinctBy(x => x.Id).ToDictionary(x => x.Id, x => x);
		this.SettingsHandler = new SettingsHandler(this);

		IEnumerable<(ushort, ReadOnlySeString)> valueTuples = Service.DataManager.GetExcelSheet<TerritoryType>()
			.Where(this.IsAllowedTerritory)
			.Select(x => ((ushort)x.RowId, x.PlaceName.ValueNullable?.Name ?? "Unknown Place"));

		foreach ((ushort rowId, ReadOnlySeString placeName) in valueTuples)
		{
			this.allowedTerritory.Add(rowId);
			this.TerritoryTypeWhitelist.Add(rowId);
			this.TerritoryPlaceNameDictionary[rowId] = ItalicRegex().Replace(placeName.ToString(), "");
		}

		this.UpdateCurrentConfig(territoryType);
		this.HandleVersionChanges();
	}

	// Allowed territory intended use IDs
	private static readonly HashSet<uint> allowedTerritoryIntendedUses = [
		0, // Hub Cities
		1, // Overworld
		13, // Residential Area
		19,
		21, // The Firmament
		23, // Gold Saucer
		44, // Leap of Faith
		46, // Ocean Fishing
		47, // The Diadem
		60, // Stellar Exploration
	];

	// Helper method to determine if a territory is allowed
	private bool IsAllowedTerritory(TerritoryType territory)
	{
		return (allowedTerritoryIntendedUses.Contains(territory.TerritoryIntendedUse.RowId) ||
		        this.TerritoryTypeWhitelist.Contains((ushort)territory.RowId)) &&
		       !territory.Name.IsEmpty &&
		       territory.RowId != 136; // Exclude test map
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

	private void HandleVersionChanges()
	{
		if (!this.VoidList.Any(x => x.Version < 1)) return;

		// Due to recent changes, the id property needs to be reset.
		// Changes will be tracked using a version property.
		this.VoidList.ForEach(x =>
		{
			if (x.Version >= 1) return;
			x.Id = 0;
			x.Version = 1;
		});

		// Save the changes
		this.Save();

		// Recreate the dictionary
		this.VoidDictionary = this.VoidList.Where(x => x.Id != 0).DistinctBy(x => x.Id)
			.ToDictionary(x => x.Id, x => x);
	}

	public void Save() => Service.PluginInterface.SavePluginConfig(this);
	[System.Text.RegularExpressions.GeneratedRegex("<italic\\(\\d+\\)>")]
	private static partial System.Text.RegularExpressions.Regex ItalicRegex();
}
