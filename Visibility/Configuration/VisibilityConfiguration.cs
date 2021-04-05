using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using Dalamud.Configuration;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using Dalamud.Plugin;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Visibility.Utils;
using Visibility.Void;

namespace Visibility.Configuration
{
	public class VisibilityConfiguration : IPluginConfiguration
	{
		public int Version { get; set; }

		public bool Enabled { get; set; }
		public bool HidePet	{ get; set; }
		public bool HidePlayer { get; set; }
		public bool HideMinion { get; set; }
		public bool HideChocobo { get; set; }
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

		[NonSerialized]
		private VisibilityPlugin _plugin;

		[NonSerialized]
		private DalamudPluginInterface _pluginInterface;

		[NonSerialized]
		private bool _showVoidListWindow;

		[NonSerialized]
		private readonly byte[][] _buffer =
		{
			new byte[16],
			new byte[16],
			new byte[128],
			new byte[128],
			new byte[128]
		};

		[NonSerialized]
		public readonly Dictionary<string, Action<int>> settingDictionary = new Dictionary<string, Action<int>>();

		[NonSerialized]
		public readonly HashSet<ushort> TerritoryTypeWhitelist = new HashSet<ushort>
		{
			792,
			898,
			899
		};

		private void ChangeSetting(string propertyName)
		{
			switch (propertyName)
			{
				case nameof(HidePet):
					_plugin.UnhidePets(ContainerType.All);
					break;
				case nameof(HidePlayer):
					_plugin.UnhidePlayers(ContainerType.All);
					break;
				case nameof(HideMinion):
					_plugin.UnhideMinions(ContainerType.All);
					break;
				case nameof(HideChocobo):
					_plugin.UnhideChocobos(ContainerType.All);
					break;
				case nameof(ShowCompanyPet):
					_plugin.UnhidePets(ContainerType.Company);
					break;
				case nameof(ShowCompanyPlayer):
					_plugin.UnhidePlayers(ContainerType.Company);
					break;
				case nameof(ShowCompanyMinion):
					_plugin.UnhideMinions(ContainerType.Company);
					break;
				case nameof(ShowCompanyChocobo):
					_plugin.UnhideChocobos(ContainerType.Company);
					break;
				case nameof(ShowPartyPet):
					_plugin.UnhidePets(ContainerType.Party);
					break;
				case nameof(ShowPartyPlayer):
					_plugin.UnhidePlayers(ContainerType.Party);
					break;
				case nameof(ShowPartyMinion):
					_plugin.UnhideMinions(ContainerType.Party);
					break;
				case nameof(ShowPartyChocobo):
					_plugin.UnhideChocobos(ContainerType.Party);
					break;
				case nameof(ShowFriendPet):
					_plugin.UnhidePets(ContainerType.Friend);
					break;
				case nameof(ShowFriendPlayer):
					_plugin.UnhidePlayers(ContainerType.Friend);
					break;
				case nameof(ShowFriendMinion):
					_plugin.UnhideMinions(ContainerType.Friend);
					break;
				case nameof(ShowFriendChocobo):
					_plugin.UnhideChocobos(ContainerType.Friend);
					break;
			}
		}
		
		private FieldInfo GetBackingField(string propertyName)
		{
			return GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
		}

		private void ChangeSetting(string propertyName, int val)
		{
			var field = GetBackingField(propertyName);
			var state = (bool) field.GetValue(this);
			field.SetValue(this, val > 1 ? !state : val > 0);
			ChangeSetting(propertyName);
		}

		public void Init(VisibilityPlugin plugin, DalamudPluginInterface pluginInterface)
		{
			_plugin = plugin;
			_pluginInterface = pluginInterface;

			settingDictionary["hidepet"] = x => ChangeSetting(nameof(HidePet), x);
			settingDictionary["hideplayer"] = x => ChangeSetting(nameof(HidePlayer), x);
			settingDictionary["showcompanypet"] = x => ChangeSetting(nameof(ShowCompanyPet), x);
			settingDictionary["showcompanyplayer"] = x => ChangeSetting(nameof(ShowCompanyPlayer), x);
			settingDictionary["showcompanychocobo"] = x => ChangeSetting(nameof(ShowCompanyChocobo), x);
			settingDictionary["showpartypet"] = x => ChangeSetting(nameof(ShowPartyPet), x);
			settingDictionary["showpartyplayer"] = x => ChangeSetting(nameof(ShowPartyPlayer), x);
			settingDictionary["showpartychocobo"] = x => ChangeSetting(nameof(ShowPartyChocobo), x);
			settingDictionary["showfriendpet"] = x => ChangeSetting(nameof(ShowFriendPet), x);
			settingDictionary["showfriendplayer"] = x => ChangeSetting(nameof(ShowFriendPlayer), x);
			settingDictionary["showfriendchocobo"] = x => ChangeSetting(nameof(ShowFriendChocobo), x);
			settingDictionary["showdeadplayer"] = x => ChangeSetting(nameof(ShowDeadPlayer), x);
		}

		public void Save()
		{
			_pluginInterface.SavePluginConfig(this);
		}

		private void Checkbox(string propertyName)
		{
			var state = (bool) GetBackingField(propertyName).GetValue(this);
			if (!ImGui.Checkbox($"###{propertyName}", ref state)) return;
			ChangeSetting(propertyName, state ? 1 : 0);
			Save();
		}

		public bool DrawConfigUi()
		{
			var drawConfig = true;

			var scale = ImGui.GetIO().FontGlobalScale;

			ImGui.SetNextWindowSize(new Vector2(500 * scale, 230 * scale), ImGuiCond.Always);
			ImGui.Begin($"{_plugin.Name} Config", ref drawConfig, ImGuiWindowFlags.NoResize);

			/*if (ImGui.Checkbox($"###{nameof(_plugin.enabled)}", ref _plugin.enabled))
			{
				Enabled = _plugin.enabled;
				Save();
			}*/

			Checkbox(nameof(Enabled));

			ImGui.SameLine();
			ImGui.Text("Enable");
			ImGui.Separator();

			ImGui.Columns(6, "###cols", false);

			ImGui.NextColumn();
			ImGui.Text("Hide all");
			ImGui.NextColumn();
			ImGui.Text("Show party");
			ImGui.NextColumn();
			ImGui.Text("Show friends");
			ImGui.NextColumn();
			ImGui.Text("Show dead");
			ImGui.NextColumn();
			ImGui.Text("Show FC");
			ImGui.NextColumn();
			ImGui.Separator();
			ImGui.Text("Pets");
			ImGui.NextColumn();
			Checkbox(nameof(HidePet));
			ImGui.NextColumn();
			Checkbox(nameof(ShowPartyPet));
			ImGui.NextColumn();
			Checkbox(nameof(ShowFriendPet));
			ImGui.NextColumn();
			ImGui.NextColumn();
			Checkbox(nameof(ShowCompanyPet));
			ImGui.NextColumn();

			ImGui.Text("Chocobos");
			ImGui.NextColumn();
			Checkbox(nameof(HideChocobo));
			ImGui.NextColumn();
			Checkbox(nameof(ShowPartyChocobo));
			ImGui.NextColumn();
			Checkbox(nameof(ShowFriendChocobo));
			ImGui.NextColumn();
			ImGui.NextColumn();
			Checkbox(nameof(ShowCompanyChocobo));
			ImGui.NextColumn();

			ImGui.Text("Players");
			ImGui.NextColumn();
			Checkbox(nameof(HidePlayer));
			ImGui.NextColumn();
			Checkbox(nameof(ShowPartyPlayer));
			ImGui.NextColumn();
			Checkbox(nameof(ShowFriendPlayer));
			ImGui.NextColumn();
			Checkbox(nameof(ShowDeadPlayer));
			ImGui.NextColumn();
			Checkbox(nameof(ShowCompanyPlayer));
			ImGui.NextColumn();

			ImGui.Text("Minions");
			ImGui.NextColumn();
			Checkbox(nameof(HideMinion));
			ImGui.NextColumn();
			Checkbox(nameof(ShowPartyMinion));
			ImGui.NextColumn();
			Checkbox(nameof(ShowFriendMinion));
			ImGui.NextColumn();
			ImGui.NextColumn();
			Checkbox(nameof(ShowCompanyMinion));
			ImGui.NextColumn();
			ImGui.Separator();

			if (ImGui.Button("Refresh"))
			{
				_plugin.RefreshActors();
			}

			ImGui.NextColumn();
			ImGui.NextColumn();
			ImGui.NextColumn();
			ImGui.NextColumn();
			ImGui.NextColumn();

			if (ImGui.Button("VoidList"))
			{
				_showVoidListWindow = !_showVoidListWindow;
			}
			ImGui.NextColumn();
			ImGui.End();

			if (!_showVoidListWindow) return drawConfig;
			
			ImGui.SetNextWindowSize(new Vector2(700 * scale, 500), ImGuiCond.FirstUseEver);
			ImGui.Begin($"{_plugin.Name}: VoidList", ref _showVoidListWindow);

			var footer2 = (ImGui.GetStyle().ItemSpacing.Y) / 2 + ImGui.GetFrameHeightWithSpacing();
			ImGui.BeginChild("scrollingVoidList", new Vector2(0, -footer2), false);

			ImGui.Columns(6, "###voidCols");
			ImGui.Text("Firstname");
			ImGui.NextColumn();
			ImGui.Text("Lastname");
			ImGui.NextColumn();
			ImGui.Text("World");
			ImGui.NextColumn();
			ImGui.Text("Date");
			ImGui.NextColumn();
			ImGui.Text("Reason");
			ImGui.NextColumn();
			ImGui.NextColumn();

			VoidItem itemToRemove = null;

			foreach (var item in VoidList)
			{
				ImGui.TextUnformatted(item.Firstname);
				ImGui.NextColumn();
				ImGui.TextUnformatted(item.Lastname);
				ImGui.NextColumn();
				ImGui.TextUnformatted(item.HomeworldName);
				ImGui.NextColumn();
				ImGui.Text(item.Time.ToString(CultureInfo.CurrentCulture));
				ImGui.NextColumn();
				ImGui.TextUnformatted(item.Reason);
				ImGui.NextColumn();

				if (ImGui.Button($"Remove##{item.Name}"))
				{
					itemToRemove = item;
				}

				ImGui.NextColumn();
			}

			if (itemToRemove != null)
			{
				if (_pluginInterface.ClientState.Actors
					.SingleOrDefault(x => x is PlayerCharacter
					                      && itemToRemove.ActorId != 0
					                      && x.ObjectKind != ObjectKind.Companion
					                      && x.ActorId == itemToRemove.ActorId) is PlayerCharacter actor)
				{
					actor.Render();
				}

				VoidList.Remove(itemToRemove);
				Save();
			}

			var manual = true;

			if (_pluginInterface.ClientState.LocalPlayer?.TargetActorID > 0)
			{
				Array.Clear(_buffer[0], 0, _buffer[0].Length);
				Array.Clear(_buffer[1], 0, _buffer[1].Length);
				Array.Clear(_buffer[2], 0, _buffer[2].Length);

				if (_pluginInterface.ClientState.Actors
					.SingleOrDefault(x => x is PlayerCharacter
					                      && x.ObjectKind != ObjectKind.Companion
					                      && x.ActorId == _pluginInterface.ClientState.LocalPlayer?.TargetActorID) is PlayerCharacter actor)
				{
					Encoding.Default.GetBytes(actor.GetFirstname()).CopyTo(_buffer[0], 0);
					Encoding.Default.GetBytes(actor.GetLastname()).CopyTo(_buffer[1], 0);
					Encoding.Default.GetBytes(actor.HomeWorld.GameData.Name).CopyTo(_buffer[2], 0);

					manual = false;
				}
			}

			ImGui.InputText("###playerFirstName", _buffer[0], (uint) _buffer[0].Length, ImGuiInputTextFlags.CharsNoBlank);
			ImGui.NextColumn();
			ImGui.InputText("###playerLastName", _buffer[1], (uint) _buffer[1].Length, ImGuiInputTextFlags.CharsNoBlank);
			ImGui.NextColumn();
			ImGui.InputText("###homeworldName", _buffer[2], (uint) _buffer[2].Length, ImGuiInputTextFlags.CharsNoBlank);
			ImGui.NextColumn();
			ImGui.NextColumn();
			ImGui.InputText("###reason", _buffer[3], (uint) _buffer[3].Length);
			ImGui.NextColumn();

			if (ImGui.Button("Void player"))
			{
				_plugin.VoidPlayer(manual ? "VoidUIManual" : string.Empty,
					$"{_buffer[0].ByteToString()} {_buffer[1].ByteToString()} {_buffer[2].ByteToString()} {_buffer[3].ByteToString()}");

				foreach (var item in _buffer)
					Array.Clear(item, 0, item.Length);
			}

			ImGui.NextColumn();

			ImGui.EndChild();

			ImGui.End();

			return drawConfig;
		}
	}
}
