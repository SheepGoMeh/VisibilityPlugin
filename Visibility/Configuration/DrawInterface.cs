using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using ImGuiNET;
using Visibility.Void;

namespace Visibility.Configuration
{
	public partial class VisibilityConfiguration
	{
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

			ImGui.SetNextWindowSize(new Vector2(500 * scale, 262 * scale), ImGuiCond.Always);
			ImGui.Begin($"{_plugin.Name} Config", ref drawConfig, ImGuiWindowFlags.NoResize);

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

			ImGui.Columns(1, "###cols", false);

			Checkbox(nameof(HideStar));
			ImGui.SameLine();
			ImGui.Text("Hide non-party Earthly Star");
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip("Hides Earthly Star not belonging to players in your party (Only works in combat)");
			}

			ImGui.NextColumn();
			ImGui.Separator();

			ImGui.Columns(6, "###cols", false);

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
						                      && x.ActorId == _pluginInterface.ClientState.LocalPlayer
							                      ?.TargetActorID) is
					PlayerCharacter actor)
				{
					Encoding.Default.GetBytes(actor.GetFirstname()).CopyTo(_buffer[0], 0);
					Encoding.Default.GetBytes(actor.GetLastname()).CopyTo(_buffer[1], 0);
					Encoding.Default.GetBytes(actor.HomeWorld.GameData.Name).CopyTo(_buffer[2], 0);

					manual = false;
				}
			}

			ImGui.InputText("###playerFirstName", _buffer[0], (uint) _buffer[0].Length,
				ImGuiInputTextFlags.CharsNoBlank);
			ImGui.NextColumn();
			ImGui.InputText("###playerLastName", _buffer[1], (uint) _buffer[1].Length,
				ImGuiInputTextFlags.CharsNoBlank);
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