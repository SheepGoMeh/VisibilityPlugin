using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ImGuiNET;
using Visibility.Void;

namespace Visibility.Configuration
{
	public partial class VisibilityConfiguration
	{
		private static readonly Vector4 VersionColor = new Vector4(.5f, .5f, .5f, 1f);

		private static readonly string VersionString =
			System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!.ToString();

		private readonly IEnumerable<VoidItem>[] sortedContainer = new IEnumerable<VoidItem>[2];

		private readonly bool[] sortAscending = { true, true };
		private readonly Func<VoidItem, object>[] sortKeySelector = new Func<VoidItem, object>[2];

		private void CenteredCheckbox(string propertyName)
		{
			var state = (bool)this.GetBackingField(propertyName).GetValue(this)!;
			ImGui.SetCursorPosX(
				ImGui.GetCursorPosX() +
				((ImGui.GetColumnWidth() + (2 * ImGui.GetStyle().FramePadding.X)) / 2) -
				(2 * ImGui.GetStyle().ItemSpacing.X) - (2 * ImGui.GetStyle().CellPadding.X));

			if (!ImGui.Checkbox($"###{propertyName}", ref state))
			{
				return;
			}

			this.ChangeSetting(propertyName, state ? 1 : 0);
			this.Save();
		}

		private void Checkbox(string propertyName)
		{
			var state = (bool)this.GetBackingField(propertyName).GetValue(this)!;

			if (!ImGui.Checkbox($"###{propertyName}", ref state))
			{
				return;
			}

			this.ChangeSetting(propertyName, state ? 1 : 0);
			this.Save();
		}

		public bool DrawConfigUi()
		{
			var drawConfig = true;

			ImGui.SetNextWindowSize(new Vector2(700 * ImGui.GetIO().FontGlobalScale, 0), ImGuiCond.Always);

			if (ImGui.Begin($"{VisibilityPlugin.Instance.Name} Config", ref drawConfig, ImGuiWindowFlags.NoResize))
			{
				this.Checkbox(nameof(this.Enabled));

				ImGui.SameLine();
				ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionEnable);
				var cursorY = ImGui.GetCursorPosY();
				var comboWidth =
					(ImGui.CalcTextSize(
						VisibilityPlugin.Instance.PluginLocalization.GetString("LanguageName", Localization.Language.English)).X * 2) +
					(ImGui.GetStyle().ItemSpacing.X * ImGui.GetIO().FontGlobalScale);
				ImGui.SameLine(
					(ImGui.GetContentRegionMax().X / 2) -
					ImGui.CalcTextSize(VisibilityPlugin.Instance.PluginLocalization.OptionLanguage).X - comboWidth);
				ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionLanguage);
				ImGui.SameLine();
				ImGui.PushItemWidth(comboWidth);
				if (ImGui.BeginCombo("###language", VisibilityPlugin.Instance.PluginLocalization.LanguageName))
				{
					foreach (var language in VisibilityPlugin.Instance.PluginLocalization.AvailableLanguages.Where(
						         language =>
							         ImGui.Selectable(
								         VisibilityPlugin.Instance.PluginLocalization.GetString("LanguageName", language))))
					{
						VisibilityPlugin.Instance.Configuration.Language = language;
						VisibilityPlugin.Instance.PluginLocalization.CurrentLanguage = language;
						this.Save();
					}

					ImGui.EndCombo();
				}

				ImGui.PopItemWidth();
				ImGui.SameLine(
					ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - ImGui.CalcTextSize(VersionString).X -
					ImGui.GetScrollX());
				ImGui.SetCursorPosY(cursorY / 2);
				ImGui.TextColored(VersionColor, VersionString);
				ImGui.SetCursorPosY(cursorY);

				if (ImGui.BeginTable("###cols", 6, ImGuiTableFlags.BordersOuterH))
				{
					ImGui.TableSetupColumn(
						string.Empty,
						ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthStretch);
					ImGui.TableSetupColumn(
						VisibilityPlugin.Instance.PluginLocalization.OptionHideAll,
						ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthStretch);
					ImGui.TableSetupColumn(
						VisibilityPlugin.Instance.PluginLocalization.OptionShowParty,
						ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthStretch);
					ImGui.TableSetupColumn(
						VisibilityPlugin.Instance.PluginLocalization.OptionShowFriends,
						ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthStretch);
					ImGui.TableSetupColumn(
						VisibilityPlugin.Instance.PluginLocalization.OptionShowFc,
						ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthStretch);
					ImGui.TableSetupColumn(
						VisibilityPlugin.Instance.PluginLocalization.OptionShowDead,
						ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthStretch);
					ImGui.TableHeadersRow();

					ImGui.TableNextColumn();
					ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionPlayers);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(nameof(this.HidePlayer));
					ImGui.TableNextColumn();
					this.CenteredCheckbox(nameof(this.ShowPartyPlayer));
					ImGui.TableNextColumn();
					this.CenteredCheckbox(nameof(this.ShowFriendPlayer));
					ImGui.TableNextColumn();
					this.CenteredCheckbox(nameof(this.ShowCompanyPlayer));
					ImGui.TableNextColumn();
					this.CenteredCheckbox(nameof(this.ShowDeadPlayer));
					ImGui.TableNextRow();

					ImGui.TableNextColumn();
					ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionPets);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(nameof(this.HidePet));
					ImGui.TableNextColumn();
					this.CenteredCheckbox(nameof(this.ShowPartyPet));
					ImGui.TableNextColumn();
					this.CenteredCheckbox(nameof(this.ShowFriendPet));
					ImGui.TableNextColumn();
					this.CenteredCheckbox(nameof(this.ShowCompanyPet));
					ImGui.TableNextRow();

					ImGui.TableNextColumn();
					ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionChocobos);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(nameof(this.HideChocobo));
					ImGui.TableNextColumn();
					this.CenteredCheckbox(nameof(this.ShowPartyChocobo));
					ImGui.TableNextColumn();
					this.CenteredCheckbox(nameof(this.ShowFriendChocobo));
					ImGui.TableNextColumn();
					this.CenteredCheckbox(nameof(this.ShowCompanyChocobo));
					ImGui.TableNextRow();

					ImGui.TableNextColumn();
					ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionMinions);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(nameof(this.HideMinion));
					ImGui.TableNextColumn();
					this.CenteredCheckbox(nameof(this.ShowPartyMinion));
					ImGui.TableNextColumn();
					this.CenteredCheckbox(nameof(this.ShowFriendMinion));
					ImGui.TableNextColumn();
					this.CenteredCheckbox(nameof(this.ShowCompanyMinion));
					ImGui.TableNextRow();

					ImGui.EndTable();
				}

				this.Checkbox(nameof(this.HideStar));
				ImGui.SameLine();
				ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionEarthlyStar);
				if (ImGui.IsItemHovered())
				{
					ImGui.SetTooltip(VisibilityPlugin.Instance.PluginLocalization.OptionEarthlyStarTip);
				}

				ImGui.NextColumn();
				ImGui.Separator();

				ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().ItemSpacing.Y);

				if (ImGui.Button(VisibilityPlugin.Instance.PluginLocalization.OptionRefresh))
				{
					VisibilityPlugin.Instance.RefreshActors();
				}

				ImGui.SameLine(
					ImGui.GetContentRegionMax().X - ImGui.CalcTextSize(VisibilityPlugin.Instance.PluginLocalization.WhitelistName).X -
					ImGui.CalcTextSize(VisibilityPlugin.Instance.PluginLocalization.VoidListName).X -
					(4 * ImGui.GetStyle().FramePadding.X) -
					(ImGui.GetStyle().ItemSpacing.X * ImGui.GetIO().FontGlobalScale));

				if (ImGui.Button(VisibilityPlugin.Instance.PluginLocalization.WhitelistName))
				{
					this.showListWindow[1] = !this.showListWindow[1];
				}

				ImGui.SameLine(
					ImGui.GetContentRegionMax().X - ImGui.CalcTextSize(VisibilityPlugin.Instance.PluginLocalization.VoidListName).X -
					(2 * ImGui.GetStyle().FramePadding.X));

				if (ImGui.Button(VisibilityPlugin.Instance.PluginLocalization.VoidListName))
				{
					this.showListWindow[0] = !this.showListWindow[0];
				}
			}

			ImGui.End();

			if (this.showListWindow[0])
			{
				this.DrawVoidList();
			}

			if (this.showListWindow[1])
			{
				this.DrawWhitelist();
			}

			return drawConfig;
		}

		private static IEnumerable<VoidItem> SortContainer(
			IEnumerable<VoidItem> container,
			Func<VoidItem, object> keySelector,
			bool isAscending,
			out Func<VoidItem, object> keySelectorOut)
		{
			keySelectorOut = keySelector;
			return isAscending ? container.OrderBy(keySelector) : container.OrderByDescending(keySelector);
		}

		private void DrawVoidList()
		{
			ImGui.SetNextWindowSize(new Vector2(700, 500), ImGuiCond.FirstUseEver);
			if (!ImGui.Begin(
				    $"{VisibilityPlugin.Instance.Name}: {VisibilityPlugin.Instance.PluginLocalization.VoidListName}",
				    ref this.showListWindow[0]))
			{
				ImGui.End();
				return;
			}

			if (ImGui.BeginTable(
				    "VoidListTable",
				    6,
				    ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable))
			{
				ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.ColumnFirstname);
				ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.ColumnLastname);
				ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.ColumnWorld);
				ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.ColumnDate, ImGuiTableColumnFlags.DefaultSort);
				ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.ColumnReason);
				ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.ColumnAction, ImGuiTableColumnFlags.NoSort);
				ImGui.TableSetupScrollFreeze(0, 1);
				ImGui.TableHeadersRow();

				VoidItem? itemToRemove = null;

				this.sortedContainer[0] ??= this.VoidList;

				var sortSpecs = ImGui.TableGetSortSpecs();

				if (sortSpecs.SpecsDirty)
				{
					this.sortAscending[0] = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending;

					switch (sortSpecs.Specs.ColumnIndex)
					{
						case 0:
							this.sortedContainer[0] = SortContainer(
								this.VoidList,
								x => x.Firstname,
								this.sortAscending[0],
								out this.sortKeySelector[0]);
							break;
						case 1:
							this.sortedContainer[0] = SortContainer(
								this.VoidList,
								x => x.Lastname,
								this.sortAscending[0],
								out this.sortKeySelector[0]);
							break;
						case 2:
							this.sortedContainer[0] = SortContainer(
								this.VoidList,
								x => x.HomeworldName,
								this.sortAscending[0],
								out this.sortKeySelector[0]);
							break;
						case 3:
							this.sortedContainer[0] = SortContainer(
								this.VoidList,
								x => x.Time,
								this.sortAscending[0],
								out this.sortKeySelector[0]);
							break;
						case 4:
							this.sortedContainer[0] = SortContainer(
								this.VoidList,
								x => x.Reason,
								this.sortAscending[0],
								out this.sortKeySelector[0]);
							break;
					}

					sortSpecs.SpecsDirty = false;
				}

				foreach (var item in this.sortedContainer[0])
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

					if (ImGui.Button($"{VisibilityPlugin.Instance.PluginLocalization.OptionRemovePlayer}##{item.Name}"))
					{
						itemToRemove = item;
					}

					ImGui.TableNextRow();
				}

				if (itemToRemove != null)
				{
					if (VisibilityPlugin.ObjectTable
						    .SingleOrDefault(
							    x =>
								    x is PlayerCharacter playerCharacter &&
								    playerCharacter.Name.TextValue.Equals(
									    itemToRemove.Name,
									    StringComparison.Ordinal) &&
								    playerCharacter.HomeWorld.Id == itemToRemove.HomeworldId) is PlayerCharacter a)
					{
						a.Render();
					}

					this.VoidList.Remove(itemToRemove);
					this.Save();

					this.sortedContainer[0] = SortContainer(
						this.VoidList,
						this.sortKeySelector[0],
						this.sortAscending[0],
						out this.sortKeySelector[0]);
				}

				var manual = true;

				if (VisibilityPlugin.ClientState.LocalPlayer?.TargetObjectId > 0
				    && VisibilityPlugin.ObjectTable
						    .SingleOrDefault(
							    x => x is PlayerCharacter
							         && x.ObjectKind != ObjectKind.Companion
							         && x.ObjectId == VisibilityPlugin.ClientState.LocalPlayer
								         ?.TargetObjectId) is
					    PlayerCharacter actor)
				{
					Array.Clear(this.buffer[0], 0, this.buffer[0].Length);
					Array.Clear(this.buffer[1], 0, this.buffer[1].Length);
					Array.Clear(this.buffer[2], 0, this.buffer[2].Length);

					Encoding.Default.GetBytes(actor.GetFirstname()).CopyTo(this.buffer[0], 0);
					Encoding.Default.GetBytes(actor.GetLastname()).CopyTo(this.buffer[1], 0);
					Encoding.Default.GetBytes(actor.HomeWorld.GameData!.Name).CopyTo(this.buffer[2], 0);

					manual = false;
				}

				ImGui.TableNextColumn();
				ImGui.InputText(
					"###playerFirstName",
					this.buffer[0],
					(uint)this.buffer[0].Length,
					ImGuiInputTextFlags.CharsNoBlank);
				ImGui.TableNextColumn();
				ImGui.InputText(
					"###playerLastName",
					this.buffer[1],
					(uint)this.buffer[1].Length,
					ImGuiInputTextFlags.CharsNoBlank);
				ImGui.TableNextColumn();
				ImGui.InputText(
					"###homeworldName",
					this.buffer[2],
					(uint)this.buffer[2].Length,
					ImGuiInputTextFlags.CharsNoBlank);
				ImGui.TableNextColumn();
				ImGui.TableNextColumn();
				ImGui.InputText("###reason", this.buffer[3], (uint)this.buffer[3].Length);
				ImGui.TableNextColumn();

				if (ImGui.Button(VisibilityPlugin.Instance.PluginLocalization.OptionAddPlayer))
				{
					VisibilityPlugin.Instance.VoidPlayer(
						manual ? "VoidUIManual" : string.Empty,
						$"{this.buffer[0].ByteToString()} {this.buffer[1].ByteToString()} {this.buffer[2].ByteToString()} {this.buffer[3].ByteToString()}");

					foreach (var item in this.buffer)
					{
						Array.Clear(item, 0, item.Length);
					}

					this.sortedContainer[0] = SortContainer(
						this.VoidList,
						this.sortKeySelector[0],
						this.sortAscending[0],
						out this.sortKeySelector[0]);
				}

				ImGui.EndTable();
			}

			ImGui.End();
		}

		private void DrawWhitelist()
		{
			ImGui.SetNextWindowSize(new Vector2(700, 500), ImGuiCond.FirstUseEver);
			if (!ImGui.Begin(
				    $"{VisibilityPlugin.Instance.Name}: {VisibilityPlugin.Instance.PluginLocalization.WhitelistName}",
				    ref this.showListWindow[1]))
			{
				ImGui.End();
				return;
			}

			if (ImGui.BeginTable(
				    "WhitelistTable",
				    6,
				    ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable))
			{
				ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.ColumnFirstname);
				ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.ColumnLastname);
				ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.ColumnWorld);
				ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.ColumnDate, ImGuiTableColumnFlags.DefaultSort);
				ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.ColumnReason);
				ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.ColumnAction, ImGuiTableColumnFlags.NoSort);
				ImGui.TableSetupScrollFreeze(0, 1);
				ImGui.TableHeadersRow();

				VoidItem? itemToRemove = null;

				this.sortedContainer[1] ??= this.Whitelist;

				var sortSpecs = ImGui.TableGetSortSpecs();

				if (sortSpecs.SpecsDirty)
				{
					this.sortAscending[1] = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending;

					switch (sortSpecs.Specs.ColumnIndex)
					{
						case 0:
							this.sortedContainer[1] = SortContainer(
								this.Whitelist,
								x => x.Firstname,
								this.sortAscending[1],
								out this.sortKeySelector[1]);
							break;
						case 1:
							this.sortedContainer[1] = SortContainer(
								this.Whitelist,
								x => x.Lastname,
								this.sortAscending[1],
								out this.sortKeySelector[1]);
							break;
						case 2:
							this.sortedContainer[1] = SortContainer(
								this.Whitelist,
								x => x.HomeworldName,
								this.sortAscending[1],
								out this.sortKeySelector[1]);
							break;
						case 3:
							this.sortedContainer[1] = SortContainer(
								this.Whitelist,
								x => x.Time,
								this.sortAscending[1],
								out this.sortKeySelector[1]);
							break;
						case 4:
							this.sortedContainer[1] = SortContainer(
								this.Whitelist,
								x => x.Reason,
								this.sortAscending[1],
								out this.sortKeySelector[1]);
							break;
					}

					sortSpecs.SpecsDirty = false;
				}

				foreach (var item in this.sortedContainer[1])
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

					if (ImGui.Button($"{VisibilityPlugin.Instance.PluginLocalization.OptionRemovePlayer}##{item.Name}"))
					{
						itemToRemove = item;
					}

					ImGui.TableNextRow();
				}

				if (itemToRemove != null)
				{
					this.Whitelist.Remove(itemToRemove);
					this.Save();
					this.sortedContainer[1] = SortContainer(
						this.Whitelist,
						this.sortKeySelector[1],
						this.sortAscending[1],
						out this.sortKeySelector[1]);
				}

				var manual = true;

				if (VisibilityPlugin.ClientState.LocalPlayer?.TargetObjectId > 0
				    && VisibilityPlugin.ObjectTable
						    .SingleOrDefault(
							    x => x is PlayerCharacter
							         && x.ObjectKind != ObjectKind.Companion
							         && x.ObjectId == VisibilityPlugin.ClientState.LocalPlayer
								         ?.TargetObjectId) is
					    PlayerCharacter actor)
				{
					Array.Clear(this.buffer[4], 0, this.buffer[4].Length);
					Array.Clear(this.buffer[5], 0, this.buffer[5].Length);
					Array.Clear(this.buffer[6], 0, this.buffer[6].Length);

					Encoding.Default.GetBytes(actor.GetFirstname()).CopyTo(this.buffer[4], 0);
					Encoding.Default.GetBytes(actor.GetLastname()).CopyTo(this.buffer[5], 0);
					Encoding.Default.GetBytes(actor.HomeWorld.GameData!.Name).CopyTo(this.buffer[6], 0);

					manual = false;
				}

				ImGui.TableNextColumn();
				ImGui.InputText(
					"###playerFirstName",
					this.buffer[4],
					(uint)this.buffer[4].Length,
					ImGuiInputTextFlags.CharsNoBlank);
				ImGui.TableNextColumn();
				ImGui.InputText(
					"###playerLastName",
					this.buffer[5],
					(uint)this.buffer[5].Length,
					ImGuiInputTextFlags.CharsNoBlank);
				ImGui.TableNextColumn();
				ImGui.InputText(
					"###homeworldName",
					this.buffer[6],
					(uint)this.buffer[6].Length,
					ImGuiInputTextFlags.CharsNoBlank);
				ImGui.TableNextColumn();
				ImGui.TableNextColumn();
				ImGui.InputText("###reason", this.buffer[7], (uint)this.buffer[7].Length);
				ImGui.TableNextColumn();

				if (ImGui.Button(VisibilityPlugin.Instance.PluginLocalization.OptionAddPlayer))
				{
					VisibilityPlugin.Instance.WhitelistPlayer(
						manual ? "WhitelistUIManual" : string.Empty,
						$"{this.buffer[4].ByteToString()} {this.buffer[5].ByteToString()} {this.buffer[6].ByteToString()} {this.buffer[7].ByteToString()}");

					foreach (var item in this.buffer)
					{
						Array.Clear(item, 0, item.Length);
					}

					this.sortedContainer[1] = SortContainer(
						this.Whitelist,
						this.sortKeySelector[1],
						this.sortAscending[1],
						out this.sortKeySelector[1]);
				}

				ImGui.EndTable();
			}

			ImGui.End();
		}
	}
}