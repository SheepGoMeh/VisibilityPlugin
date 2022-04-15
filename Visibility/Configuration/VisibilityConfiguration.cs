using System;
using System.Collections.Generic;
using System.Reflection;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Visibility.Utils;
using Visibility.Void;

namespace Visibility.Configuration
{
	public partial class VisibilityConfiguration : IPluginConfiguration
	{
		public int Version { get; set; }

		public Localization.Language Language { get; set; }

		public bool Enabled { get; set; }
		public bool HidePet	{ get; set; }
		public bool HidePlayer { get; set; }
		public bool HideMinion { get; set; }
		public bool HideChocobo { get; set; }
		public bool HideStar { get; set; }
		public bool EnableContextMenu { get; set; }
		public bool ShowCompanyPet { get; set; }
		public bool ShowCompanyPlayer { get; set; }
		public bool ShowCompanyMinion { get; set; }
		public bool ShowCompanyChocobo { get; set; }
		public bool ShowPartyPet { get; set; }
		public bool ShowPartyPlayer { get; set; }
		public bool ShowPartyMinion { get; set; }
		public bool ShowPartyChocobo { get; set; }
		public bool ShowFriendPet { get; set; }
		public bool ShowFriendPlayer { get; set; }
		public bool ShowFriendMinion { get; set; }
		public bool ShowFriendChocobo { get; set; }
		public bool ShowDeadPlayer { get; set; }

		public List<VoidItem> VoidList { get; } = new List<VoidItem>();
		public List<VoidItem> Whitelist { get; } = new List<VoidItem>();

		[NonSerialized]
		private VisibilityPlugin? _plugin;

		[NonSerialized]
		private bool[] _showListWindow = {false, false};

		[NonSerialized]
		private readonly byte[][] _buffer =
		{
			new byte[16],
			new byte[16],
			new byte[128],
			new byte[128],
			new byte[16],
			new byte[16],
			new byte[128],
			new byte[128]
		};

		[NonSerialized]
		public readonly Dictionary<string, Action<int>> settingDictionary = new Dictionary<string, Action<int>>();

		[NonSerialized]
		public readonly HashSet<ushort> TerritoryTypeWhitelist = new HashSet<ushort>
		{
			792, // The Fall of Belah'dia 
			899, // The Falling City of Nym
			900, // The Endeavor
			939, // The Diadem
		};

		private void ChangeSetting(string propertyName)
		{
			switch (propertyName)
			{
				case nameof(Enabled):
					if (!_plugin!.Disable) // Make sure the disable event is finished before enabling again
					{
						_plugin.Disable = !Enabled;
					}
					break;
				case nameof(HidePet):
					_plugin!.ShowPets(ContainerType.All);
					break;
				case nameof(HidePlayer):
					_plugin!.ShowPlayers(ContainerType.All);
					break;
				case nameof(HideMinion):
					_plugin!.ShowMinions(ContainerType.All);
					break;
				case nameof(HideChocobo):
					_plugin!.ShowChocobos(ContainerType.All);
					break;
				case nameof(EnableContextMenu):
					_plugin!.PluginContextMenu.Toggle();
					break;
				case nameof(ShowCompanyPet):
					_plugin!.ShowPets(ContainerType.Company);
					break;
				case nameof(ShowCompanyPlayer):
					_plugin!.ShowPlayers(ContainerType.Company);
					break;
				case nameof(ShowCompanyMinion):
					_plugin!.ShowMinions(ContainerType.Company);
					break;
				case nameof(ShowCompanyChocobo):
					_plugin!.ShowChocobos(ContainerType.Company);
					break;
				case nameof(ShowPartyPet):
					_plugin!.ShowPets(ContainerType.Party);
					break;
				case nameof(ShowPartyPlayer):
					_plugin!.ShowPlayers(ContainerType.Party);
					break;
				case nameof(ShowPartyMinion):
					_plugin!.ShowMinions(ContainerType.Party);
					break;
				case nameof(ShowPartyChocobo):
					_plugin!.ShowChocobos(ContainerType.Party);
					break;
				case nameof(ShowFriendPet):
					_plugin!.ShowPets(ContainerType.Friend);
					break;
				case nameof(ShowFriendPlayer):
					_plugin!.ShowPlayers(ContainerType.Friend);
					break;
				case nameof(ShowFriendMinion):
					_plugin!.ShowMinions(ContainerType.Friend);
					break;
				case nameof(ShowFriendChocobo):
					_plugin!.ShowChocobos(ContainerType.Friend);
					break;
			}
		}
		
		private FieldInfo GetBackingField(string propertyName)
		{
			return GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
		}

		private void ChangeSetting(string propertyName, int val)
		{
			var field = GetBackingField(propertyName);
			var state = (bool) field.GetValue(this)!;
			field.SetValue(this, val > 1 ? !state : val > 0);
			ChangeSetting(propertyName);
		}

		public void Init(VisibilityPlugin plugin)
		{
			_plugin = plugin;

			settingDictionary["enabled"] = x => ChangeSetting(nameof(Enabled), x);
			settingDictionary["hidepet"] = x => ChangeSetting(nameof(HidePet), x);
			settingDictionary["hidestar"] = x => ChangeSetting(nameof(HideStar), x);
			settingDictionary["hideplayer"] = x => ChangeSetting(nameof(HidePlayer), x);
			settingDictionary["hidechocobo"] = x => ChangeSetting(nameof(HideChocobo), x);
			settingDictionary["hideminion"] = x => ChangeSetting(nameof(HideMinion), x);
			settingDictionary["showcompanypet"] = x => ChangeSetting(nameof(ShowCompanyPet), x);
			settingDictionary["showcompanyplayer"] = x => ChangeSetting(nameof(ShowCompanyPlayer), x);
			settingDictionary["showcompanychocobo"] = x => ChangeSetting(nameof(ShowCompanyChocobo), x);
			settingDictionary["showcompanyminion"] = x => ChangeSetting(nameof(ShowCompanyMinion), x);
			settingDictionary["showpartypet"] = x => ChangeSetting(nameof(ShowPartyPet), x);
			settingDictionary["showpartyplayer"] = x => ChangeSetting(nameof(ShowPartyPlayer), x);
			settingDictionary["showpartychocobo"] = x => ChangeSetting(nameof(ShowPartyChocobo), x);
			settingDictionary["showpartyminion"] = x => ChangeSetting(nameof(ShowPartyMinion), x);
			settingDictionary["showfriendpet"] = x => ChangeSetting(nameof(ShowFriendPet), x);
			settingDictionary["showfriendplayer"] = x => ChangeSetting(nameof(ShowFriendPlayer), x);
			settingDictionary["showfriendchocobo"] = x => ChangeSetting(nameof(ShowFriendChocobo), x);
			settingDictionary["showfriendminion"] = x => ChangeSetting(nameof(ShowFriendMinion), x);
			settingDictionary["showdeadplayer"] = x => ChangeSetting(nameof(ShowDeadPlayer), x);
		}

		public void Save()
		{
			_plugin!.PluginInterface.SavePluginConfig(this);
		}
	}
}
