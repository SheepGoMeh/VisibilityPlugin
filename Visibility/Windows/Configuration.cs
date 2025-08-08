using System.Linq;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Windowing;

using Dalamud.Bindings.ImGui;

using Visibility.Configuration;
using Visibility.Utils;

namespace Visibility.Windows;

public class Configuration: Window
{
	public Configuration(WindowSystem windowSystem): base($"{VisibilityPlugin.Instance.Name} Config", ImGuiWindowFlags.NoResize, true)
	{
		this.whitelistWindow = new VoidItemList(isWhitelist: true);
		this.voidItemListWindow = new VoidItemList(isWhitelist: false);

		windowSystem.AddWindow(this.whitelistWindow);
		windowSystem.AddWindow(this.voidItemListWindow);

		this.Size = new Vector2(700 * ImGui.GetIO().FontGlobalScale, 0);
		this.SizeCondition = ImGuiCond.Always;
	}

	private static readonly Vector4 versionColor = new(.5f, .5f, .5f, 1f);

	private static readonly string versionString =
		System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!.ToString();

	private bool comboNewOpen;
	private readonly byte[] buffer = new byte[128];

	private readonly VoidItemList whitelistWindow;
	private readonly VoidItemList voidItemListWindow;

	public override void Draw()
	{
		VisibilityConfiguration configuration = VisibilityPlugin.Instance.Configuration;
		ImGuiElements.Checkbox(configuration.Enabled, nameof(configuration.Enabled));

		ImGui.SameLine();
		ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionEnable);
		float cursorY = ImGui.GetCursorPosY();

		ImGui.SameLine();
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 50);
		if (configuration.AdvancedEnabled)
		{
			ushort territoryType = configuration.CurrentEditedConfig.TerritoryType;

			ImGui.SetNextItemWidth(250f);
			if (ImGuiElements.ComboWithFilter(
				    "##currentConfig",
				    ref territoryType,
				    ref this.comboNewOpen,
				    configuration.TerritoryPlaceNameDictionary,
				    this.buffer,
				    5,
				    FontAwesomeIcon.Search.ToIconString(),
				    UiBuilder.IconFont))
			{
				configuration.UpdateCurrentConfig(territoryType, true);
			}

			ImGui.SameLine();
			if (configuration.CurrentConfig != configuration.CurrentEditedConfig)
			{
				if (ImGui.Button(VisibilityPlugin.Instance.PluginLocalization.ResetToCurrentArea))
				{
					configuration.CurrentEditedConfig = configuration.CurrentConfig;
				}
			}
		}

		ImGui.SameLine(
			ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - ImGui.CalcTextSize(versionString).X -
			ImGui.GetScrollX());
		ImGui.SetCursorPosY(cursorY / 2);
		ImGui.TextColored(versionColor, versionString);
		ImGui.SetCursorPosY(cursorY);

		if (ImGui.BeginTable("###cols", 7, ImGuiTableFlags.BordersOuterH))
		{
			ImGui.TableSetupColumn(
				string.Empty,
				ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthStretch);
			ImGui.TableSetupColumn(
				VisibilityPlugin.Instance.PluginLocalization.OptionHideAll,
				ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthStretch);
			ImGui.TableSetupColumn(
				"Hide in combat",
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
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.HidePlayer,
				nameof(configuration.CurrentEditedConfig.HidePlayer));
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.HidePlayerInCombat,
				nameof(configuration.CurrentEditedConfig.HidePlayerInCombat));
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.ShowPartyPlayer,
				nameof(configuration.CurrentEditedConfig.ShowPartyPlayer));
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.ShowFriendPlayer,
				nameof(configuration.CurrentEditedConfig.ShowFriendPlayer));
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.ShowCompanyPlayer,
				nameof(configuration.CurrentEditedConfig.ShowCompanyPlayer));
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.ShowDeadPlayer,
				nameof(configuration.CurrentEditedConfig.ShowDeadPlayer));
			ImGui.TableNextRow();

			ImGui.TableNextColumn();
			ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionPets);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.HidePet,
				nameof(configuration.CurrentEditedConfig.HidePet));
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.HidePetInCombat,
				nameof(configuration.CurrentEditedConfig.HidePetInCombat));
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.ShowPartyPet,
				nameof(configuration.CurrentEditedConfig.ShowPartyPet));
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.ShowFriendPet,
				nameof(configuration.CurrentEditedConfig.ShowFriendPet));
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.ShowCompanyPet,
				nameof(configuration.CurrentEditedConfig.ShowCompanyPet));
			ImGui.TableNextRow();

			ImGui.TableNextColumn();
			ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionChocobos);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.HideChocobo,
				nameof(configuration.CurrentEditedConfig.HideChocobo));
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.HideChocoboInCombat,
				nameof(configuration.CurrentEditedConfig.HideChocoboInCombat));
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.ShowPartyChocobo,
				nameof(configuration.CurrentEditedConfig.ShowPartyChocobo));
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.ShowFriendChocobo,
				nameof(configuration.CurrentEditedConfig.ShowFriendChocobo));
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.ShowCompanyChocobo,
				nameof(configuration.CurrentEditedConfig.ShowCompanyChocobo));
			ImGui.TableNextRow();

			ImGui.TableNextColumn();
			ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionMinions);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.HideMinion,
				nameof(configuration.CurrentEditedConfig.HideMinion));
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.HideMinionInCombat,
				nameof(configuration.CurrentEditedConfig.HideMinionInCombat));
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.ShowPartyMinion,
				nameof(configuration.CurrentEditedConfig.ShowPartyMinion));
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.ShowFriendMinion,
				nameof(configuration.CurrentEditedConfig.ShowFriendMinion));
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				configuration.CurrentEditedConfig.ShowCompanyMinion,
				nameof(configuration.CurrentEditedConfig.ShowCompanyMinion));
			ImGui.TableNextRow();

			ImGui.EndTable();
		}

		ImGuiElements.Checkbox(configuration.HideStar, nameof(configuration.HideStar));
		ImGui.SameLine();
		ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionEarthlyStar);
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(VisibilityPlugin.Instance.PluginLocalization.OptionEarthlyStarTip);
		}

		ImGui.NextColumn();
		ImGuiElements.Checkbox(configuration.AdvancedEnabled, nameof(configuration.AdvancedEnabled));
		ImGui.SameLine();
		ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.AdvancedOption);
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(VisibilityPlugin.Instance.PluginLocalization.AdvancedOptionTooltip);
		}

		ImGui.NextColumn();
		float comboWidth =
			(ImGui.CalcTextSize(
					VisibilityPlugin.Instance.PluginLocalization.GetString(
						"LanguageName",
						Localization.Language.English))
				.X * 2) +
			(ImGui.GetStyle().ItemSpacing.X * ImGui.GetIO().FontGlobalScale);
		ImGui.SameLine(
			(ImGui.GetContentRegionMax().X / 2) -
			ImGui.CalcTextSize(VisibilityPlugin.Instance.PluginLocalization.OptionLanguage).X - comboWidth);
		ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionLanguage);
		ImGui.SameLine();
		ImGui.PushItemWidth(comboWidth);
		if (ImGui.BeginCombo("###language", VisibilityPlugin.Instance.PluginLocalization.LanguageName))
		{
			foreach (Localization.Language language in VisibilityPlugin.Instance.PluginLocalization.AvailableLanguages
				         .Where(
					         language =>
						         ImGui.Selectable(
							         VisibilityPlugin.Instance.PluginLocalization.GetString("LanguageName", language))))
			{
				VisibilityPlugin.Instance.Configuration.Language = language;
				VisibilityPlugin.Instance.PluginLocalization.CurrentLanguage = language;
				configuration.Save();
			}

			ImGui.EndCombo();
		}

		ImGui.PopItemWidth();
		ImGui.NextColumn();
		ImGuiElements.Checkbox(configuration.EnableContextMenu, nameof(configuration.EnableContextMenu));
		ImGui.SameLine();
		ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionContextMenu);
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(VisibilityPlugin.Instance.PluginLocalization.OptionContextMenuTip);
		}

		ImGui.NextColumn();
		
		ImGuiElements.Checkbox(configuration.ShowTargetOfTarget, nameof(configuration.ShowTargetOfTarget));
		ImGui.SameLine();
		ImGui.Text(VisibilityPlugin.Instance.PluginLocalization.OptionShowTargetOfTarget);
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(VisibilityPlugin.Instance.PluginLocalization.OptionShowTargetOfTargetTip);
		}
		ImGui.NextColumn();
		ImGui.Separator();

		ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().ItemSpacing.Y);

		if (ImGui.Button(VisibilityPlugin.Instance.PluginLocalization.OptionRefresh))
		{
			VisibilityPlugin.Instance.RefreshActors();
		}

		ImGui.SameLine(
			ImGui.GetContentRegionMax().X -
			ImGui.CalcTextSize(VisibilityPlugin.Instance.PluginLocalization.WhitelistName).X -
			ImGui.CalcTextSize(VisibilityPlugin.Instance.PluginLocalization.VoidListName).X -
			(4 * ImGui.GetStyle().FramePadding.X) -
			(ImGui.GetStyle().ItemSpacing.X * ImGui.GetIO().FontGlobalScale));

		if (ImGui.Button(VisibilityPlugin.Instance.PluginLocalization.WhitelistName))
		{
			this.whitelistWindow.Toggle();
		}

		ImGui.SameLine(
			ImGui.GetContentRegionMax().X -
			ImGui.CalcTextSize(VisibilityPlugin.Instance.PluginLocalization.VoidListName).X -
			(2 * ImGui.GetStyle().FramePadding.X));

		if (ImGui.Button(VisibilityPlugin.Instance.PluginLocalization.VoidListName))
		{
			this.voidItemListWindow.Toggle();
		}
	}
}
