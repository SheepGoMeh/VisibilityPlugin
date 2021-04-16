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
		private static readonly Vector4 VersionColor = new Vector4(.5f, .5f, .5f, 1f);

		private static readonly string VersionString =
			System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

		private void CenteredCheckbox(string propertyName)
		{
			var state = (bool) GetBackingField(propertyName).GetValue(this);
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() +
			                    (ImGui.GetColumnWidth() + 2 * ImGui.GetStyle().FramePadding.X) / 2 -
			                    2 * ImGui.GetStyle().ItemSpacing.X - 2 * ImGui.GetStyle().CellPadding.X);
			if (!ImGui.Checkbox($"###{propertyName}", ref state)) return;
			ChangeSetting(propertyName, state ? 1 : 0);
			Save();
		}

		private void Checkbox(string propertyName)
		{
			var state = (bool) GetBackingField(propertyName).GetValue(this);
			if (!ImGui.Checkbox($"###{propertyName}", ref state)) return;
			ChangeSetting(propertyName, state ? 1 : 0);
			Save();
		}

		private static void CenteredText(string text)
		{
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() - ImGui.CalcTextSize(text).X) / 2 -
			                    2 * ImGui.GetStyle().CellPadding.X);
			ImGui.Text(text);
		}

		public bool DrawConfigUi()
		{
			var drawConfig = true;

			var scale = ImGui.GetIO().FontGlobalScale;

			ImGui.SetNextWindowSize(new Vector2(500 * scale, 0), ImGuiCond.Always);
			ImGui.Begin($"{_plugin.Name} Config", ref drawConfig, ImGuiWindowFlags.NoResize);

			Checkbox(nameof(Enabled));

			ImGui.SameLine();
			ImGui.Text("Enable");
			var cursorY = ImGui.GetCursorPosY();
			ImGui.SameLine(ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - ImGui.CalcTextSize(VersionString).X -
			               ImGui.GetScrollX());
			ImGui.SetCursorPosY(cursorY / 2);
			ImGui.TextColored(VersionColor, VersionString);
			ImGui.SetCursorPosY(cursorY);
			ImGui.Separator();

			ImGui.Columns(6, "###cols", false);

			ImGui.NextColumn();
			CenteredText("Hide all");
			ImGui.NextColumn();
			CenteredText("Show party");
			ImGui.NextColumn();
			CenteredText("Show friends");
			ImGui.NextColumn();
			CenteredText("Show FC");
			ImGui.NextColumn();
			CenteredText("Show dead");
			ImGui.NextColumn();
			ImGui.Separator();

			ImGui.Text("Players");
			ImGui.NextColumn();
			CenteredCheckbox(nameof(HidePlayer));
			ImGui.NextColumn();
			CenteredCheckbox(nameof(ShowPartyPlayer));
			ImGui.NextColumn();
			CenteredCheckbox(nameof(ShowFriendPlayer));
			ImGui.NextColumn();
			CenteredCheckbox(nameof(ShowCompanyPlayer));
			ImGui.NextColumn();
			CenteredCheckbox(nameof(ShowDeadPlayer));
			ImGui.NextColumn();

			ImGui.Text("Pets");
			ImGui.NextColumn();
			CenteredCheckbox(nameof(HidePet));
			ImGui.NextColumn();
			CenteredCheckbox(nameof(ShowPartyPet));
			ImGui.NextColumn();
			CenteredCheckbox(nameof(ShowFriendPet));
			ImGui.NextColumn();
			CenteredCheckbox(nameof(ShowCompanyPet));
			ImGui.NextColumn();
			ImGui.NextColumn();

			ImGui.Text("Chocobos");
			ImGui.NextColumn();
			CenteredCheckbox(nameof(HideChocobo));
			ImGui.NextColumn();
			CenteredCheckbox(nameof(ShowPartyChocobo));
			ImGui.NextColumn();
			CenteredCheckbox(nameof(ShowFriendChocobo));
			ImGui.NextColumn();
			CenteredCheckbox(nameof(ShowCompanyChocobo));
			ImGui.NextColumn();
			ImGui.NextColumn();

			ImGui.Text("Minions");
			ImGui.NextColumn();
			CenteredCheckbox(nameof(HideMinion));
			ImGui.NextColumn();
			CenteredCheckbox(nameof(ShowPartyMinion));
			ImGui.NextColumn();
			CenteredCheckbox(nameof(ShowFriendMinion));
			ImGui.NextColumn();
			CenteredCheckbox(nameof(ShowCompanyMinion));
			ImGui.NextColumn();
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

			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().ItemSpacing.Y);

			if (ImGui.Button("Refresh"))
			{
				_plugin.RefreshActors();
			}

			ImGui.SameLine(ImGui.GetContentRegionMax().X - ImGui.CalcTextSize("Whitelist").X -
			               ImGui.CalcTextSize("VoidList").X - 4 * ImGui.GetStyle().FramePadding.X -
			               ImGui.GetStyle().ItemSpacing.X * scale);

			if (ImGui.Button("Whitelist"))
			{
				_showListWindow[1] = !_showListWindow[1];
			}

			ImGui.SameLine(ImGui.GetContentRegionMax().X - ImGui.CalcTextSize("VoidList").X -
			               2 * ImGui.GetStyle().FramePadding.X);

			if (ImGui.Button("VoidList"))
			{
				_showListWindow[0] = !_showListWindow[0];
			}

			ImGui.End();

			if (_showListWindow[0])
			{
				ImGui.SetNextWindowSize(new Vector2(700 * scale, 500), ImGuiCond.FirstUseEver);
				ImGui.Begin($"{_plugin.Name}: VoidList", ref _showListWindow[0]);

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
						                      && x.ActorId == itemToRemove.ActorId) is PlayerCharacter a)
					{
						a.Render();
					}

					VoidList.Remove(itemToRemove);
					Save();
				}

				var manual = true;

				if (_pluginInterface.ClientState.LocalPlayer?.TargetActorID > 0
				    && _pluginInterface.ClientState.Actors
						    .SingleOrDefault(x => x is PlayerCharacter
						                          && x.ObjectKind != ObjectKind.Companion
						                          && x.ActorId == _pluginInterface.ClientState.LocalPlayer
							                          ?.TargetActorID) is
					    PlayerCharacter actor)
				{
					Array.Clear(_buffer[0], 0, _buffer[0].Length);
					Array.Clear(_buffer[1], 0, _buffer[1].Length);
					Array.Clear(_buffer[2], 0, _buffer[2].Length);

					Encoding.Default.GetBytes(actor.GetFirstname()).CopyTo(_buffer[0], 0);
					Encoding.Default.GetBytes(actor.GetLastname()).CopyTo(_buffer[1], 0);
					Encoding.Default.GetBytes(actor.HomeWorld.GameData.Name).CopyTo(_buffer[2], 0);

					manual = false;
				}

				ImGui.InputText("###playerFirstName", _buffer[0], (uint) _buffer[0].Length,
					ImGuiInputTextFlags.CharsNoBlank);
				ImGui.NextColumn();
				ImGui.InputText("###playerLastName", _buffer[1], (uint) _buffer[1].Length,
					ImGuiInputTextFlags.CharsNoBlank);
				ImGui.NextColumn();
				ImGui.InputText("###homeworldName", _buffer[2], (uint) _buffer[2].Length,
					ImGuiInputTextFlags.CharsNoBlank);
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
			}

			if (_showListWindow[1])
			{
				ImGui.SetNextWindowSize(new Vector2(700 * scale, 500), ImGuiCond.FirstUseEver);
				ImGui.Begin($"{_plugin.Name}: Whitelist", ref _showListWindow[1]);

				var footer2 = (ImGui.GetStyle().ItemSpacing.Y) / 2 + ImGui.GetFrameHeightWithSpacing();
				ImGui.BeginChild("scrollingWhitelist", new Vector2(0, -footer2), false);

				ImGui.Columns(6, "###whitelistCols");
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

				foreach (var item in Whitelist)
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
					Whitelist.Remove(itemToRemove);
					Save();
				}

				var manual = true;

				if (_pluginInterface.ClientState.LocalPlayer?.TargetActorID > 0
				    && _pluginInterface.ClientState.Actors
						    .SingleOrDefault(x => x is PlayerCharacter
						                          && x.ObjectKind != ObjectKind.Companion
						                          && x.ActorId == _pluginInterface.ClientState.LocalPlayer
							                          ?.TargetActorID) is
					    PlayerCharacter actor)
				{
					Array.Clear(_buffer[4], 0, _buffer[4].Length);
					Array.Clear(_buffer[5], 0, _buffer[5].Length);
					Array.Clear(_buffer[6], 0, _buffer[6].Length);

					Encoding.Default.GetBytes(actor.GetFirstname()).CopyTo(_buffer[4], 0);
					Encoding.Default.GetBytes(actor.GetLastname()).CopyTo(_buffer[5], 0);
					Encoding.Default.GetBytes(actor.HomeWorld.GameData.Name).CopyTo(_buffer[6], 0);

					manual = false;
				}

				ImGui.InputText("###playerFirstName", _buffer[4], (uint) _buffer[4].Length,
					ImGuiInputTextFlags.CharsNoBlank);
				ImGui.NextColumn();
				ImGui.InputText("###playerLastName", _buffer[5], (uint) _buffer[5].Length,
					ImGuiInputTextFlags.CharsNoBlank);
				ImGui.NextColumn();
				ImGui.InputText("###homeworldName", _buffer[6], (uint) _buffer[6].Length,
					ImGuiInputTextFlags.CharsNoBlank);
				ImGui.NextColumn();
				ImGui.NextColumn();
				ImGui.InputText("###reason", _buffer[7], (uint) _buffer[7].Length);
				ImGui.NextColumn();

				if (ImGui.Button("Add player"))
				{
					_plugin.WhitelistPlayer(manual ? "WhitelistUIManual" : string.Empty,
						$"{_buffer[4].ByteToString()} {_buffer[5].ByteToString()} {_buffer[6].ByteToString()} {_buffer[7].ByteToString()}");

					foreach (var item in _buffer)
						Array.Clear(item, 0, item.Length);
				}

				ImGui.NextColumn();

				ImGui.EndChild();
			}

			ImGui.End();

			return drawConfig;
		}
	}
}