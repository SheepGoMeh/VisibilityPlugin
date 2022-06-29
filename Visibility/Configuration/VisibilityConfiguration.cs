using System;
using System.Collections.Generic;
using System.Reflection;
using Dalamud.Configuration;
using Visibility.Utils;
using Visibility.Void;

namespace Visibility.Configuration
{
	public partial class VisibilityConfiguration : IPluginConfiguration
	{
		public int Version { get; set; }

		public Localization.Language Language { get; private set; }

		public bool Enabled { get; set; }
		public bool HidePet { get; set; }
		public bool HidePlayer { get; set; }
		public bool HideMinion { get; set; }
		public bool HideChocobo { get; set; }
		public bool HideStar { get; set; }
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

		private void ChangeSetting(string propertyName)
		{
			switch (propertyName)
			{
				case nameof(this.Enabled):
					if (!VisibilityPlugin.Instance.Disable) // Make sure the disable event is finished before enabling again
					{
						VisibilityPlugin.Instance.Disable = !this.Enabled;
					}
					break;
				case nameof(this.HidePet):
					VisibilityPlugin.Instance.ShowPets(ContainerType.All);
					break;
				case nameof(this.HidePlayer):
					VisibilityPlugin.Instance.ShowPlayers(ContainerType.All);
					break;
				case nameof(this.HideMinion):
					VisibilityPlugin.Instance.ShowMinions(ContainerType.All);
					break;
				case nameof(this.HideChocobo):
					VisibilityPlugin.Instance.ShowChocobos(ContainerType.All);
					break;
				case nameof(this.ShowCompanyPet):
					VisibilityPlugin.Instance.ShowPets(ContainerType.Company);
					break;
				case nameof(this.ShowCompanyPlayer):
					VisibilityPlugin.Instance.ShowPlayers(ContainerType.Company);
					break;
				case nameof(this.ShowCompanyMinion):
					VisibilityPlugin.Instance.ShowMinions(ContainerType.Company);
					break;
				case nameof(this.ShowCompanyChocobo):
					VisibilityPlugin.Instance.ShowChocobos(ContainerType.Company);
					break;
				case nameof(this.ShowPartyPet):
					VisibilityPlugin.Instance.ShowPets(ContainerType.Party);
					break;
				case nameof(this.ShowPartyPlayer):
					VisibilityPlugin.Instance.ShowPlayers(ContainerType.Party);
					break;
				case nameof(this.ShowPartyMinion):
					VisibilityPlugin.Instance.ShowMinions(ContainerType.Party);
					break;
				case nameof(this.ShowPartyChocobo):
					VisibilityPlugin.Instance.ShowChocobos(ContainerType.Party);
					break;
				case nameof(this.ShowFriendPet):
					VisibilityPlugin.Instance.ShowPets(ContainerType.Friend);
					break;
				case nameof(this.ShowFriendPlayer):
					VisibilityPlugin.Instance.ShowPlayers(ContainerType.Friend);
					break;
				case nameof(this.ShowFriendMinion):
					VisibilityPlugin.Instance.ShowMinions(ContainerType.Friend);
					break;
				case nameof(this.ShowFriendChocobo):
					VisibilityPlugin.Instance.ShowChocobos(ContainerType.Friend);
					break;
			}
		}
		
		private FieldInfo GetBackingField(string propertyName)
		{
			return this.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
		}

		private void ChangeSetting(string propertyName, int val)
		{
			var field = this.GetBackingField(propertyName);
			var state = (bool) field.GetValue(this)!;
			field.SetValue(this, val > 1 ? !state : val > 0);
			this.ChangeSetting(propertyName);
		}

		public void Init()
		{
			this.SettingDictionary["enabled"] = x => this.ChangeSetting(nameof(this.Enabled), x);
			this.SettingDictionary["hidepet"] = x => this.ChangeSetting(nameof(this.HidePet), x);
			this.SettingDictionary["hidestar"] = x => this.ChangeSetting(nameof(this.HideStar), x);
			this.SettingDictionary["hideplayer"] = x => this.ChangeSetting(nameof(this.HidePlayer), x);
			this.SettingDictionary["hidechocobo"] = x => this.ChangeSetting(nameof(this.HideChocobo), x);
			this.SettingDictionary["hideminion"] = x => this.ChangeSetting(nameof(this.HideMinion), x);
			this.SettingDictionary["showcompanypet"] = x => this.ChangeSetting(nameof(this.ShowCompanyPet), x);
			this.SettingDictionary["showcompanyplayer"] = x => this.ChangeSetting(nameof(this.ShowCompanyPlayer), x);
			this.SettingDictionary["showcompanychocobo"] = x => this.ChangeSetting(nameof(this.ShowCompanyChocobo), x);
			this.SettingDictionary["showcompanyminion"] = x => this.ChangeSetting(nameof(this.ShowCompanyMinion), x);
			this.SettingDictionary["showpartypet"] = x => this.ChangeSetting(nameof(this.ShowPartyPet), x);
			this.SettingDictionary["showpartyplayer"] = x => this.ChangeSetting(nameof(this.ShowPartyPlayer), x);
			this.SettingDictionary["showpartychocobo"] = x => this.ChangeSetting(nameof(this.ShowPartyChocobo), x);
			this.SettingDictionary["showpartyminion"] = x => this.ChangeSetting(nameof(this.ShowPartyMinion), x);
			this.SettingDictionary["showfriendpet"] = x => this.ChangeSetting(nameof(this.ShowFriendPet), x);
			this.SettingDictionary["showfriendplayer"] = x => this.ChangeSetting(nameof(this.ShowFriendPlayer), x);
			this.SettingDictionary["showfriendchocobo"] = x => this.ChangeSetting(nameof(this.ShowFriendChocobo), x);
			this.SettingDictionary["showfriendminion"] = x => this.ChangeSetting(nameof(this.ShowFriendMinion), x);
			this.SettingDictionary["showdeadplayer"] = x => this.ChangeSetting(nameof(this.ShowDeadPlayer), x);
		}

		public void Save()
		{
			VisibilityPlugin.PluginInterface.SavePluginConfig(this);
		}
	}
}
