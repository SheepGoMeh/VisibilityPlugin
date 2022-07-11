using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
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

		private void CenteredCheckbox(
			ref bool property,
			bool edit = false,
			[CallerArgumentExpression("property")] string propertyName = "")
		{
			ImGui.SetCursorPosX(
				ImGui.GetCursorPosX() +
				((ImGui.GetColumnWidth() + (2 * ImGui.GetStyle().FramePadding.X)) / 2) -
				(2 * ImGui.GetStyle().ItemSpacing.X) - (2 * ImGui.GetStyle().CellPadding.X));

			if (!ImGui.Checkbox($"###{propertyName}", ref property))
			{
				return;
			}

			this.ChangeSetting(ref property, property ? 1 : 0, edit, propertyName);
			this.Save();
		}

		private void Checkbox(
			ref bool property,
			bool edit = false,
			[CallerArgumentExpression("property")] string propertyName = "")
		{
			if (!ImGui.Checkbox($"###{propertyName}", ref property))
			{
				return;
			}

			this.ChangeSetting(ref property, property ? 1 : 0, edit, propertyName);
			this.Save();
		}

		public bool DrawConfigUi()
		{
			var drawConfig = true;

			ImGui.SetNextWindowSize(new Vector2(700 * ImGui.GetIO().FontGlobalScale, 0), ImGuiCond.Always);

			if (ImGui.Begin($"{VisibilityPlugin.Instance.Name} Config", ref drawConfig, ImGuiWindowFlags.NoResize))
			{
				this.Checkbox(ref this.Enabled);

				ImGui.SameLine();
				ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionEnable);
				var cursorY = ImGui.GetCursorPosY();

				ImGui.SameLine();
				ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 50);
				if (this.AdvancedEnabled)
				{
					var territoryType = this.currentEditedConfig.TerritoryType;
					
					ImGui.SetNextItemWidth(250f);
					if (this.ComboWithFilter("##currentConfig", ref territoryType, this.territoryPlaceNameDictionary, this.buffer[8]))
					{
						this.UpdateCurrentConfig(territoryType, true);
					}

					ImGui.SameLine();
					if (this.CurrentConfig != this.currentEditedConfig)
					{
						if (ImGui.Button("Reset to current area"))
						{
							this.currentEditedConfig = this.CurrentConfig;
						}
					}
				}

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
					this.CenteredCheckbox(ref this.currentEditedConfig.HidePlayer, this.CurrentConfig != this.currentEditedConfig);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(ref this.currentEditedConfig.ShowPartyPlayer, this.CurrentConfig != this.currentEditedConfig);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(ref this.currentEditedConfig.ShowFriendPlayer, this.CurrentConfig != this.currentEditedConfig);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(ref this.currentEditedConfig.ShowCompanyPlayer, this.CurrentConfig != this.currentEditedConfig);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(ref this.currentEditedConfig.ShowDeadPlayer, this.CurrentConfig != this.currentEditedConfig);
					ImGui.TableNextRow();

					ImGui.TableNextColumn();
					ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionPets);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(ref this.currentEditedConfig.HidePet, this.CurrentConfig != this.currentEditedConfig);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(ref this.currentEditedConfig.ShowPartyPet, this.CurrentConfig != this.currentEditedConfig);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(ref this.currentEditedConfig.ShowFriendPet, this.CurrentConfig != this.currentEditedConfig);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(ref this.currentEditedConfig.ShowCompanyPet, this.CurrentConfig != this.currentEditedConfig);
					ImGui.TableNextRow();

					ImGui.TableNextColumn();
					ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionChocobos);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(ref this.currentEditedConfig.HideChocobo, this.CurrentConfig != this.currentEditedConfig);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(ref this.currentEditedConfig.ShowPartyChocobo, this.CurrentConfig != this.currentEditedConfig);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(ref this.currentEditedConfig.ShowFriendChocobo, this.CurrentConfig != this.currentEditedConfig);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(ref this.currentEditedConfig.ShowCompanyChocobo, this.CurrentConfig != this.currentEditedConfig);
					ImGui.TableNextRow();

					ImGui.TableNextColumn();
					ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionMinions);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(ref this.currentEditedConfig.HideMinion, this.CurrentConfig != this.currentEditedConfig);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(ref this.currentEditedConfig.ShowPartyMinion, this.CurrentConfig != this.currentEditedConfig);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(ref this.currentEditedConfig.ShowFriendMinion, this.CurrentConfig != this.currentEditedConfig);
					ImGui.TableNextColumn();
					this.CenteredCheckbox(ref this.currentEditedConfig.ShowCompanyMinion, this.CurrentConfig != this.currentEditedConfig);
					ImGui.TableNextRow();

					ImGui.EndTable();
				}

				this.Checkbox(ref this.currentEditedConfig.HideStar);
				ImGui.SameLine();
				ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionEarthlyStar);
				if (ImGui.IsItemHovered())
				{
					ImGui.SetTooltip(VisibilityPlugin.Instance.PluginLocalization.OptionEarthlyStarTip);
				}

				ImGui.NextColumn();
				this.Checkbox(ref this.AdvancedEnabled);
				ImGui.SameLine();
				ImGui.Text("Advanced options");
				
				ImGui.NextColumn();
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
	}
}