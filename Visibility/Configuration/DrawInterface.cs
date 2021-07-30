using System;
using System.Collections.Generic;
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

		private readonly IEnumerable<VoidItem>[] _sortedContainer = new IEnumerable<VoidItem>[2];

		private readonly bool[] _sortAscending = {true, true};
		private readonly Func<VoidItem, object>[] _sortKeySelector = new Func<VoidItem, object>[2];

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

			ImGui.SetNextWindowSize(new Vector2(500 * ImGui.GetIO().FontGlobalScale, 0), ImGuiCond.Always);

			if (ImGui.Begin($"{_plugin.Name} Config", ref drawConfig, ImGuiWindowFlags.NoResize))
			{
				Checkbox(nameof(Enabled));

				ImGui.SameLine();
				ImGui.Text(_plugin.PluginLocalization.OptionEnable);
				var cursorY = ImGui.GetCursorPosY();
				ImGui.SameLine(ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - ImGui.CalcTextSize(VersionString).X -
				               ImGui.GetScrollX());
				ImGui.SetCursorPosY(cursorY / 2);
				ImGui.TextColored(VersionColor, VersionString);
				ImGui.SetCursorPosY(cursorY);
				ImGui.Separator();

				ImGui.Columns(6, "###cols", false);

				ImGui.NextColumn();
				CenteredText(_plugin.PluginLocalization.OptionHideAll);
				ImGui.NextColumn();
				CenteredText(_plugin.PluginLocalization.OptionShowParty);
				ImGui.NextColumn();
				CenteredText(_plugin.PluginLocalization.OptionShowFriends);
				ImGui.NextColumn();
				CenteredText(_plugin.PluginLocalization.OptionShowFc);
				ImGui.NextColumn();
				CenteredText(_plugin.PluginLocalization.OptionShowDead);
				ImGui.NextColumn();
				ImGui.Separator();

				ImGui.Text(_plugin.PluginLocalization.OptionPlayers);
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

				ImGui.Text(_plugin.PluginLocalization.OptionPets);
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

				ImGui.Text(_plugin.PluginLocalization.OptionChocobos);
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

				ImGui.Text(_plugin.PluginLocalization.OptionMinions);
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
				ImGui.Text(_plugin.PluginLocalization.OptionEarthlyStar);
				if (ImGui.IsItemHovered())
				{
					ImGui.SetTooltip(_plugin.PluginLocalization.OptionEarthlyStarTip);
				}

				ImGui.NextColumn();

				Checkbox(nameof(EnableContextMenu));
				ImGui.SameLine();
				ImGui.Text(_plugin.PluginLocalization.OptionContextMenu);
				if (ImGui.IsItemHovered())
				{
					ImGui.SetTooltip(_plugin.PluginLocalization.OptionContextMenuTip);
				}

				ImGui.NextColumn();
				ImGui.Separator();

				ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().ItemSpacing.Y);

				if (ImGui.Button(_plugin.PluginLocalization.OptionRefresh))
				{
					_plugin.RefreshActors();
				}

				ImGui.SameLine(ImGui.GetContentRegionMax().X - ImGui.CalcTextSize(_plugin.PluginLocalization.WhitelistName).X -
				               ImGui.CalcTextSize(_plugin.PluginLocalization.VoidListName).X - 4 * ImGui.GetStyle().FramePadding.X -
				               ImGui.GetStyle().ItemSpacing.X * ImGui.GetIO().FontGlobalScale);

				if (ImGui.Button(_plugin.PluginLocalization.WhitelistName))
				{
					_showListWindow[1] = !_showListWindow[1];
				}

				ImGui.SameLine(ImGui.GetContentRegionMax().X - ImGui.CalcTextSize(_plugin.PluginLocalization.VoidListName).X -
				               2 * ImGui.GetStyle().FramePadding.X);

				if (ImGui.Button(_plugin.PluginLocalization.VoidListName))
				{
					_showListWindow[0] = !_showListWindow[0];
				}
			}

			ImGui.End();

			if (_showListWindow[0])
			{
				DrawVoidList();
			}

			if (_showListWindow[1])
			{
				DrawWhitelist();
			}

			return drawConfig;
		}

		private static IEnumerable<VoidItem> SortContainer(IEnumerable<VoidItem> container,
			Func<VoidItem, object> keySelector, bool isAscending, out Func<VoidItem, object> keySelectorOut)
		{
			keySelectorOut = keySelector;
			return keySelector == null ? container :
				isAscending ? container.OrderBy(keySelector) : container.OrderByDescending(keySelector);
		}

		private void DrawVoidList()
		{
			ImGui.SetNextWindowSize(new Vector2(700, 500), ImGuiCond.FirstUseEver);
			if (!ImGui.Begin($"{_plugin.Name}: {_plugin.PluginLocalization.VoidListName}", ref _showListWindow[0]))
			{
				ImGui.End();
				return;
			}

			if (ImGui.BeginTable("VoidListTable", 6,
				ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable))
			{
				ImGui.TableSetupColumn(_plugin.PluginLocalization.ColumnFirstname);
				ImGui.TableSetupColumn(_plugin.PluginLocalization.ColumnLastname);
				ImGui.TableSetupColumn(_plugin.PluginLocalization.ColumnWorld);
				ImGui.TableSetupColumn(_plugin.PluginLocalization.ColumnDate, ImGuiTableColumnFlags.DefaultSort);
				ImGui.TableSetupColumn(_plugin.PluginLocalization.ColumnReason);
				ImGui.TableSetupColumn(_plugin.PluginLocalization.ColumnAction, ImGuiTableColumnFlags.NoSort);
				ImGui.TableSetupScrollFreeze(0, 1);
				ImGui.TableHeadersRow();

				VoidItem itemToRemove = null;

				if (_sortedContainer[0] == null)
				{
					_sortedContainer[0] = VoidList;
				}

				var sortSpecs = ImGui.TableGetSortSpecs();

				if (sortSpecs.SpecsDirty)
				{
					_sortAscending[0] = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending;

					switch (sortSpecs.Specs.ColumnIndex)
					{
						case 0:
							_sortedContainer[0] = SortContainer(VoidList, x => x.Firstname,
								_sortAscending[0], out _sortKeySelector[0]);
							break;
						case 1:
							_sortedContainer[0] = SortContainer(VoidList, x => x.Lastname,
								_sortAscending[0], out _sortKeySelector[0]);
							break;
						case 2:
							_sortedContainer[0] = SortContainer(VoidList, x => x.HomeworldName,
								_sortAscending[0], out _sortKeySelector[0]);
							break;
						case 3:
							_sortedContainer[0] = SortContainer(VoidList, x => x.Time,
								_sortAscending[0], out _sortKeySelector[0]);
							break;
						case 4:
							_sortedContainer[0] = SortContainer(VoidList, x => x.Reason,
								_sortAscending[0], out _sortKeySelector[0]);
							break;
					}

					sortSpecs.SpecsDirty = false;
				}

				foreach (var item in _sortedContainer[0])
				{
					ImGui.TableNextColumn();
					ImGui.TextUnformatted(item.Firstname);
					ImGui.TableNextColumn();
					ImGui.TextUnformatted(item.Lastname);
					ImGui.TableNextColumn();
					ImGui.TextUnformatted(item.HomeworldName);
					ImGui.TableNextColumn();
					ImGui.Text(item.Time.ToString(CultureInfo.CurrentCulture));
					ImGui.TableNextColumn();
					ImGui.TextUnformatted(item.Reason);
					ImGui.TableNextColumn();

					if (ImGui.Button($"Remove##{item.Name}"))
					{
						itemToRemove = item;
					}

					ImGui.TableNextRow();
				}

				if (itemToRemove != null)
				{
					if (_pluginInterface.ClientState.Actors
						.SingleOrDefault(x =>
							x is PlayerCharacter playerCharacter &&
							playerCharacter.Name.Equals(itemToRemove.Name, StringComparison.Ordinal) &&
							playerCharacter.HomeWorld.Id == itemToRemove.HomeworldId) is PlayerCharacter a)
					{
						a.Render();
					}

					VoidList.Remove(itemToRemove);
					Save();

					_sortedContainer[0] = SortContainer(VoidList, _sortKeySelector[0], _sortAscending[0],
						out _sortKeySelector[0]);
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

				ImGui.TableNextColumn();
				ImGui.InputText("###playerFirstName", _buffer[0], (uint) _buffer[0].Length,
					ImGuiInputTextFlags.CharsNoBlank);
				ImGui.TableNextColumn();
				ImGui.InputText("###playerLastName", _buffer[1], (uint) _buffer[1].Length,
					ImGuiInputTextFlags.CharsNoBlank);
				ImGui.TableNextColumn();
				ImGui.InputText("###homeworldName", _buffer[2], (uint) _buffer[2].Length,
					ImGuiInputTextFlags.CharsNoBlank);
				ImGui.TableNextColumn();
				ImGui.TableNextColumn();
				ImGui.InputText("###reason", _buffer[3], (uint) _buffer[3].Length);
				ImGui.TableNextColumn();

				if (ImGui.Button("Void player"))
				{
					_plugin.VoidPlayer(manual ? "VoidUIManual" : string.Empty,
						$"{_buffer[0].ByteToString()} {_buffer[1].ByteToString()} {_buffer[2].ByteToString()} {_buffer[3].ByteToString()}");

					foreach (var item in _buffer)
						Array.Clear(item, 0, item.Length);

					_sortedContainer[0] = SortContainer(VoidList, _sortKeySelector[0], _sortAscending[0],
						out _sortKeySelector[0]);
				}

				ImGui.EndTable();
			}

			ImGui.End();
		}

		private void DrawWhitelist()
		{
			ImGui.SetNextWindowSize(new Vector2(700, 500), ImGuiCond.FirstUseEver);
			if (!ImGui.Begin($"{_plugin.Name}: {_plugin.PluginLocalization.WhitelistName}", ref _showListWindow[1]))
			{
				ImGui.End();
				return;
			}

			if (ImGui.BeginTable("WhitelistTable", 6,
				ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable))
			{
				ImGui.TableSetupColumn(_plugin.PluginLocalization.ColumnFirstname);
				ImGui.TableSetupColumn(_plugin.PluginLocalization.ColumnLastname);
				ImGui.TableSetupColumn(_plugin.PluginLocalization.ColumnWorld);
				ImGui.TableSetupColumn(_plugin.PluginLocalization.ColumnDate, ImGuiTableColumnFlags.DefaultSort);
				ImGui.TableSetupColumn(_plugin.PluginLocalization.ColumnReason);
				ImGui.TableSetupColumn(_plugin.PluginLocalization.ColumnAction, ImGuiTableColumnFlags.NoSort);
				ImGui.TableSetupScrollFreeze(0, 1);
				ImGui.TableHeadersRow();

				VoidItem itemToRemove = null;

				if (_sortedContainer[1] == null)
				{
					_sortedContainer[1] = Whitelist;
				}

				var sortSpecs = ImGui.TableGetSortSpecs();

				if (sortSpecs.SpecsDirty)
				{
					_sortAscending[1] = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending;

					switch (sortSpecs.Specs.ColumnIndex)
					{
						case 0:
							_sortedContainer[1] = SortContainer(Whitelist, x => x.Firstname,
								_sortAscending[1], out _sortKeySelector[1]);
							break;
						case 1:
							_sortedContainer[1] = SortContainer(Whitelist, x => x.Lastname,
								_sortAscending[1], out _sortKeySelector[1]);
							break;
						case 2:
							_sortedContainer[1] = SortContainer(Whitelist, x => x.HomeworldName,
								_sortAscending[1], out _sortKeySelector[1]);
							break;
						case 3:
							_sortedContainer[1] = SortContainer(Whitelist, x => x.Time,
								_sortAscending[1], out _sortKeySelector[1]);
							break;
						case 4:
							_sortedContainer[1] = SortContainer(Whitelist, x => x.Reason,
								_sortAscending[1], out _sortKeySelector[1]);
							break;
					}

					sortSpecs.SpecsDirty = false;
				}

				foreach (var item in _sortedContainer[1])
				{
					ImGui.TableNextColumn();
					ImGui.TextUnformatted(item.Firstname);
					ImGui.TableNextColumn();
					ImGui.TextUnformatted(item.Lastname);
					ImGui.TableNextColumn();
					ImGui.TextUnformatted(item.HomeworldName);
					ImGui.TableNextColumn();
					ImGui.Text(item.Time.ToString(CultureInfo.CurrentCulture));
					ImGui.TableNextColumn();
					ImGui.TextUnformatted(item.Reason);
					ImGui.TableNextColumn();

					if (ImGui.Button($"Remove##{item.Name}"))
					{
						itemToRemove = item;
					}

					ImGui.TableNextRow();
				}

				if (itemToRemove != null)
				{
					Whitelist.Remove(itemToRemove);
					Save();
					_sortedContainer[1] = SortContainer(Whitelist, _sortKeySelector[1], _sortAscending[1],
						out _sortKeySelector[1]);
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

				ImGui.TableNextColumn();
				ImGui.InputText("###playerFirstName", _buffer[4], (uint) _buffer[4].Length,
					ImGuiInputTextFlags.CharsNoBlank);
				ImGui.TableNextColumn();
				ImGui.InputText("###playerLastName", _buffer[5], (uint) _buffer[5].Length,
					ImGuiInputTextFlags.CharsNoBlank);
				ImGui.TableNextColumn();
				ImGui.InputText("###homeworldName", _buffer[6], (uint) _buffer[6].Length,
					ImGuiInputTextFlags.CharsNoBlank);
				ImGui.TableNextColumn();
				ImGui.TableNextColumn();
				ImGui.InputText("###reason", _buffer[7], (uint) _buffer[7].Length);
				ImGui.TableNextColumn();

				if (ImGui.Button("Add player"))
				{
					_plugin.WhitelistPlayer(manual ? "WhitelistUIManual" : string.Empty,
						$"{_buffer[4].ByteToString()} {_buffer[5].ByteToString()} {_buffer[6].ByteToString()} {_buffer[7].ByteToString()}");

					foreach (var item in _buffer)
						Array.Clear(item, 0, item.Length);

					_sortedContainer[1] = SortContainer(Whitelist, _sortKeySelector[1], _sortAscending[1],
						out _sortKeySelector[1]);
				}

				ImGui.EndTable();
			}

			ImGui.End();
		}
	}
}