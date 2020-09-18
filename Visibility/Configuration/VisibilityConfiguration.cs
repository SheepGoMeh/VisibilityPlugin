using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Configuration;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Plugin;
using ImGuiNET;
using Visibility.Void;

namespace Visibility.Configuration
{
	public class VisibilityConfiguration : IPluginConfiguration
	{
		public int Version { get; set; }
		public bool Enabled { get; set; } = true;

		public bool HidePet
		{
			get => _hidePet;
			set => _hidePet = value;
		}

		public bool HidePlayer
		{
			get => _hidePlayer;
			set => _hidePlayer = value;
		}

		public bool HideChocobo
		{
			get => _hideChocobo;
			set => _hideChocobo = value;
		}

		public bool ShowCompanyPet
		{
			get => _showCompanyPet;
			set => _showCompanyPet = value;
		}

		public bool ShowCompanyPlayer
		{
			get => _showCompanyPlayer;
			set => _showCompanyPlayer = value;
		}

		public bool ShowCompanyChocobo
		{
			get => _showCompanyChocobo;
			set => _showCompanyChocobo = value;
		}

		public bool ShowPartyPet
		{
			get => _showPartyPet;
			set => _showPartyPet = value;
		}

		public bool ShowPartyPlayer
		{
			get => _showPartyPlayer;
			set => _showPartyPlayer = value;
		}

		public bool ShowPartyMinion
		{
			get => _showPartyMinion;
			set => _showPartyMinion = value;
		}

		public bool ShowPartyChocobo
		{
			get => _showPartyChocobo;
			set => _showPartyChocobo = value;
		}

		public bool ShowFriendPet
		{
			get => _showFriendPet;
			set => _showFriendPet = value;
		}

		public bool ShowFriendPlayer
		{
			get => _showFriendPlayer;
			set => _showFriendPlayer = value;
		}

		public bool ShowFriendMinion
		{
			get => _showFriendMinion;
			set => _showFriendMinion = value;
		}

		public bool ShowFriendChocobo
		{
			get => _showFriendChocobo;
			set => _showFriendChocobo = value;
		}

		public bool ShowDeadPlayer
		{
			get => _showDeadPlayer;
			set => _showDeadPlayer = value;
		}

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
		private bool _hidePet = false;
		[NonSerialized]
		private bool _hidePlayer = false;
		[NonSerialized]
		private bool _hideChocobo = false;
		[NonSerialized]
		private bool _showPartyPet = false;
		[NonSerialized]
		private bool _showPartyPlayer = false;
		[NonSerialized]
		private bool _showPartyMinion = false;
		[NonSerialized]
		private bool _showPartyChocobo = false;
		[NonSerialized]
		private bool _showFriendPet = false;
		[NonSerialized]
		private bool _showFriendPlayer = false;
		[NonSerialized]
		private bool _showFriendMinion = false;
		[NonSerialized]
		private bool _showFriendChocobo = false;
		[NonSerialized]
		private bool _showDeadPlayer = false;
		[NonSerialized]
		private bool _showCompanyPet = false;
		[NonSerialized]
		private bool _showCompanyPlayer = false;
		[NonSerialized]
		private bool _showCompanyChocobo = false;

		[NonSerialized]
		public readonly Dictionary<string, Action<bool>> settingDictionary = new Dictionary<string, Action<bool>>();

		[NonSerialized]
		public readonly HashSet<ushort> territoryTypeWhitelist = new HashSet<ushort>
		{
			792,
			898,
			899
		};

		public void Init(VisibilityPlugin plugin, DalamudPluginInterface pluginInterface)
		{
			_plugin = plugin;
			_pluginInterface = pluginInterface;

			settingDictionary["hidepet"] = x => _hidePet = x;
			settingDictionary["hideplayer"] = x => _hidePlayer = x;
			settingDictionary["showcompanypet"] = x => _showCompanyPet = x;
			settingDictionary["showcompanyplayer"] = x => _showCompanyPlayer = x;
			settingDictionary["showcompanychocobo"] = x => _showCompanyChocobo = x;
			settingDictionary["showpartypet"] = x => _showPartyPet = x;
			settingDictionary["showpartyplayer"] = x => _showPartyPlayer = x;
			settingDictionary["showpartychocobo"] = x => _showPartyChocobo = x;
			settingDictionary["showfriendpet"] = x => _showFriendPet = x;
			settingDictionary["showfriendplayer"] = x => _showFriendPlayer = x;
			settingDictionary["showfriendchocobo"] = x => _showFriendChocobo= x;
			settingDictionary["showdeadplayer"] = x => _showDeadPlayer = x;
		}

		public void Save()
		{
			_pluginInterface.SavePluginConfig(this);
		}

		private void Checkbox(string id, ref bool pluginVariable)
		{
			if (!ImGui.Checkbox($"###{id}", ref pluginVariable)) return;
			Save();
		}

		public bool DrawConfigUi()
		{
			var drawConfig = true;

			var scale = ImGui.GetIO().FontGlobalScale;

			ImGui.SetNextWindowSize(new Vector2(500 * scale, 205 * scale), ImGuiCond.Always);
			ImGui.Begin($"{_plugin.Name} Config", ref drawConfig, ImGuiWindowFlags.NoResize);

			if (ImGui.Checkbox($"###{nameof(_plugin.enabled)}", ref _plugin.enabled))
			{
				Enabled = _plugin.enabled;
				Save();
			}

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
			Checkbox(nameof(_hidePet), ref _hidePet);
			ImGui.NextColumn();
			Checkbox(nameof(_showPartyPet), ref _showPartyPet);
			ImGui.NextColumn();
			Checkbox(nameof(_showFriendPet), ref _showFriendPet);
			ImGui.NextColumn();
			ImGui.NextColumn();
			Checkbox(nameof(_showCompanyPet), ref _showCompanyPet);
			ImGui.NextColumn();

			ImGui.Text("Chocobos");
			ImGui.NextColumn();
			Checkbox(nameof(_hideChocobo), ref _hideChocobo);
			ImGui.NextColumn();
			Checkbox(nameof(_showPartyChocobo), ref _showPartyChocobo);
			ImGui.NextColumn();
			Checkbox(nameof(_showFriendChocobo), ref _showFriendChocobo);
			ImGui.NextColumn();
			ImGui.NextColumn();
			Checkbox(nameof(_showCompanyChocobo), ref _showCompanyChocobo);
			ImGui.NextColumn();

			ImGui.Text("Players");
			ImGui.NextColumn();
			Checkbox(nameof(_hidePlayer), ref _hidePlayer);
			ImGui.NextColumn();
			Checkbox(nameof(_showPartyPlayer), ref _showPartyPlayer);
			ImGui.NextColumn();
			Checkbox(nameof(_showFriendPlayer), ref _showFriendPlayer);
			ImGui.NextColumn();
			Checkbox(nameof(_showDeadPlayer), ref _showDeadPlayer);
			ImGui.NextColumn();
			Checkbox(nameof(_showCompanyPlayer), ref _showCompanyPlayer);
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
