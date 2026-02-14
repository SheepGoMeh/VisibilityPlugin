using System;
using System.Linq;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Windowing;

using Dalamud.Bindings.ImGui;

using Visibility.Configuration;
using Visibility.Handlers;
using Visibility.Utils;

namespace Visibility.Windows;

public class Configuration: Window
{
	private readonly VisibilityConfiguration configuration;
	private readonly Localization pluginLocalization;
	private readonly FrameworkUpdateHandler frameworkUpdateHandler;

	private readonly VoidItemList whitelistWindow;
	private readonly VoidItemList voidItemListWindow;

	public Configuration(
		WindowSystem windowSystem,
		VisibilityConfiguration configuration,
		Localization pluginLocalization,
		CommandManagerHandler commandManagerHandler,
		FrameworkHandler frameworkHandler,
		FrameworkUpdateHandler frameworkUpdateHandler): base("Visibility Config", ImGuiWindowFlags.NoResize, true)
	{
		this.configuration = configuration;
		this.pluginLocalization = pluginLocalization;
		this.frameworkUpdateHandler = frameworkUpdateHandler;

		this.whitelistWindow = new VoidItemList(
			isWhitelist: true, configuration, pluginLocalization, commandManagerHandler, frameworkHandler);
		this.voidItemListWindow = new VoidItemList(
			isWhitelist: false, configuration, pluginLocalization, commandManagerHandler, frameworkHandler);

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

	public override void Draw()
	{
		ImGuiElements.Checkbox(this.configuration.Enabled, nameof(this.configuration.Enabled), this.configuration);

		ImGui.SameLine();
		ImGui.Text(this.pluginLocalization.OptionEnable);
		float cursorY = ImGui.GetCursorPosY();

		ImGui.SameLine();
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 50);
		if (this.configuration.AdvancedEnabled)
		{
			ushort territoryType = this.configuration.CurrentEditedConfig.TerritoryType;

			ImGui.SetNextItemWidth(250f);
			if (ImGuiElements.ComboWithFilter(
				    "##currentConfig",
				    ref territoryType,
				    ref this.comboNewOpen,
				    this.configuration.TerritoryPlaceNameDictionary,
				    this.buffer,
				    5,
				    FontAwesomeIcon.Search.ToIconString(),
				    UiBuilder.IconFont))
			{
				this.configuration.UpdateCurrentConfig(territoryType, true);
			}

			ImGui.SameLine();
			if (this.configuration.CurrentConfig != this.configuration.CurrentEditedConfig)
			{
				if (ImGui.Button(this.pluginLocalization.ResetToCurrentArea))
				{
					this.configuration.CurrentEditedConfig = this.configuration.CurrentConfig;
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
				this.pluginLocalization.OptionHideAll,
				ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthStretch);
			ImGui.TableSetupColumn(
				"Hide in combat",
				ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthStretch);
			ImGui.TableSetupColumn(
				this.pluginLocalization.OptionShowParty,
				ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthStretch);
			ImGui.TableSetupColumn(
				this.pluginLocalization.OptionShowFriends,
				ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthStretch);
			ImGui.TableSetupColumn(
				this.pluginLocalization.OptionShowFc,
				ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthStretch);
			ImGui.TableSetupColumn(
				this.pluginLocalization.OptionShowDead,
				ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthStretch);
			ImGui.TableHeadersRow();

			ImGui.TableNextColumn();
			ImGui.Text(this.pluginLocalization.OptionPlayers);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.HidePlayer,
				nameof(this.configuration.CurrentEditedConfig.HidePlayer),
				this.configuration);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.HidePlayerInCombat,
				nameof(this.configuration.CurrentEditedConfig.HidePlayerInCombat),
				this.configuration);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.ShowPartyPlayer,
				nameof(this.configuration.CurrentEditedConfig.ShowPartyPlayer),
				this.configuration);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.ShowFriendPlayer,
				nameof(this.configuration.CurrentEditedConfig.ShowFriendPlayer),
				this.configuration);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.ShowCompanyPlayer,
				nameof(this.configuration.CurrentEditedConfig.ShowCompanyPlayer),
				this.configuration);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.ShowDeadPlayer,
				nameof(this.configuration.CurrentEditedConfig.ShowDeadPlayer),
				this.configuration);
			ImGui.TableNextRow();

			ImGui.TableNextColumn();
			ImGui.Text(this.pluginLocalization.OptionPets);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.HidePet,
				nameof(this.configuration.CurrentEditedConfig.HidePet),
				this.configuration);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.HidePetInCombat,
				nameof(this.configuration.CurrentEditedConfig.HidePetInCombat),
				this.configuration);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.ShowPartyPet,
				nameof(this.configuration.CurrentEditedConfig.ShowPartyPet),
				this.configuration);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.ShowFriendPet,
				nameof(this.configuration.CurrentEditedConfig.ShowFriendPet),
				this.configuration);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.ShowCompanyPet,
				nameof(this.configuration.CurrentEditedConfig.ShowCompanyPet),
				this.configuration);
			ImGui.TableNextRow();

			ImGui.TableNextColumn();
			ImGui.Text(this.pluginLocalization.OptionChocobos);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.HideChocobo,
				nameof(this.configuration.CurrentEditedConfig.HideChocobo),
				this.configuration);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.HideChocoboInCombat,
				nameof(this.configuration.CurrentEditedConfig.HideChocoboInCombat),
				this.configuration);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.ShowPartyChocobo,
				nameof(this.configuration.CurrentEditedConfig.ShowPartyChocobo),
				this.configuration);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.ShowFriendChocobo,
				nameof(this.configuration.CurrentEditedConfig.ShowFriendChocobo),
				this.configuration);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.ShowCompanyChocobo,
				nameof(this.configuration.CurrentEditedConfig.ShowCompanyChocobo),
				this.configuration);
			ImGui.TableNextRow();

			ImGui.TableNextColumn();
			ImGui.Text(this.pluginLocalization.OptionMinions);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.HideMinion,
				nameof(this.configuration.CurrentEditedConfig.HideMinion),
				this.configuration);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.HideMinionInCombat,
				nameof(this.configuration.CurrentEditedConfig.HideMinionInCombat),
				this.configuration);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.ShowPartyMinion,
				nameof(this.configuration.CurrentEditedConfig.ShowPartyMinion),
				this.configuration);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.ShowFriendMinion,
				nameof(this.configuration.CurrentEditedConfig.ShowFriendMinion),
				this.configuration);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				this.configuration.CurrentEditedConfig.ShowCompanyMinion,
				nameof(this.configuration.CurrentEditedConfig.ShowCompanyMinion),
				this.configuration);
			ImGui.TableNextRow();

			ImGui.EndTable();
		}

		ImGuiElements.Checkbox(this.configuration.HideStar, nameof(this.configuration.HideStar), this.configuration);
		ImGui.SameLine();
		ImGui.Text(this.pluginLocalization.OptionEarthlyStar);
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(this.pluginLocalization.OptionEarthlyStarTip);
		}

		ImGui.NextColumn();
		ImGuiElements.Checkbox(this.configuration.AdvancedEnabled, nameof(this.configuration.AdvancedEnabled), this.configuration);
		ImGui.SameLine();
		ImGui.Text(this.pluginLocalization.AdvancedOption);
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(this.pluginLocalization.AdvancedOptionTooltip);
		}

		ImGui.NextColumn();
		float comboWidth =
			(ImGui.CalcTextSize(
					this.pluginLocalization.GetString(
						"LanguageName",
						Localization.Language.English))
				.X * 2) +
			(ImGui.GetStyle().ItemSpacing.X * ImGui.GetIO().FontGlobalScale);
		ImGui.SameLine(
			(ImGui.GetContentRegionMax().X / 2) -
			ImGui.CalcTextSize(this.pluginLocalization.OptionLanguage).X - comboWidth);
		ImGui.Text(this.pluginLocalization.OptionLanguage);
		ImGui.SameLine();
		ImGui.PushItemWidth(comboWidth);
		if (ImGui.BeginCombo("###language", this.pluginLocalization.LanguageName))
		{
			foreach (Localization.Language language in this.pluginLocalization.AvailableLanguages
				         .Where(
					         language =>
						         ImGui.Selectable(
							         this.pluginLocalization.GetString("LanguageName", language))))
			{
				this.configuration.Language = language;
				this.pluginLocalization.CurrentLanguage = language;
				this.configuration.Save();
			}

			ImGui.EndCombo();
		}

		ImGui.PopItemWidth();
		ImGui.NextColumn();
		ImGuiElements.Checkbox(this.configuration.EnableContextMenu, nameof(this.configuration.EnableContextMenu), this.configuration);
		ImGui.SameLine();
		ImGui.Text(this.pluginLocalization.OptionContextMenu);
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(this.pluginLocalization.OptionContextMenuTip);
		}

		ImGui.NextColumn();
		
		ImGuiElements.Checkbox(this.configuration.ShowTargetOfTarget, nameof(this.configuration.ShowTargetOfTarget), this.configuration);
		ImGui.SameLine();
		ImGui.Text(this.pluginLocalization.OptionShowTargetOfTarget);
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(this.pluginLocalization.OptionShowTargetOfTargetTip);
		}
		ImGui.NextColumn();
		ImGui.Separator();

		ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().ItemSpacing.Y);

		this.DrawCrowdCullingSection();

		ImGui.Separator();

		ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().ItemSpacing.Y);

		if (ImGui.Button(this.pluginLocalization.OptionRefresh))
		{
			this.frameworkUpdateHandler.RequestRefresh();
		}

		ImGui.SameLine(
			ImGui.GetContentRegionMax().X -
			ImGui.CalcTextSize(this.pluginLocalization.WhitelistName).X -
			ImGui.CalcTextSize(this.pluginLocalization.VoidListName).X -
			(4 * ImGui.GetStyle().FramePadding.X) -
			(ImGui.GetStyle().ItemSpacing.X * ImGui.GetIO().FontGlobalScale));

		if (ImGui.Button(this.pluginLocalization.WhitelistName))
		{
			this.whitelistWindow.Toggle();
		}

		ImGui.SameLine(
			ImGui.GetContentRegionMax().X -
			ImGui.CalcTextSize(this.pluginLocalization.VoidListName).X -
			(2 * ImGui.GetStyle().FramePadding.X));

		if (ImGui.Button(this.pluginLocalization.VoidListName))
		{
			this.voidItemListWindow.Toggle();
		}
	}

	private void DrawCrowdCullingSection()
	{
		ImGuiElements.Checkbox(
			this.configuration.CrowdCullEnabled,
			nameof(this.configuration.CrowdCullEnabled),
			this.configuration);
		ImGui.SameLine();
		ImGui.Text("Crowd Culling");
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip("Hide players based on crowd density around you.");
		}

		if (!this.configuration.CrowdCullEnabled) return;

		ImGui.Indent();

		ImGuiElements.Checkbox(
			this.configuration.CrowdCullCountInside,
			nameof(this.configuration.CrowdCullCountInside),
			this.configuration);
		ImGui.SameLine();
		ImGui.Text(this.configuration.CrowdCullCountInside
			? "Count inside radius"
			: "Count outside radius");
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(this.configuration.CrowdCullThreshold > 0
				? "Direction to count players for the threshold.\nWhen reached, players in the opposite direction are hidden."
				: "Direction from which players will be hidden (no threshold).");
		}

		float radius = this.configuration.CrowdCullRadius;
		ImGui.SetNextItemWidth(200f);
		if (ImGui.SliderFloat("Radius (yalms)", ref radius, 1f, 100f, "%.0f"))
		{
			this.configuration.CrowdCullRadius = radius;
			this.configuration.Save();
		}

		int threshold = this.configuration.CrowdCullThreshold;
		ImGui.SetNextItemWidth(200f);
		if (ImGui.SliderInt("Threshold (0 = none)", ref threshold, 0, 100))
		{
			this.configuration.CrowdCullThreshold = threshold;
			this.configuration.Save();
		}

		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(
				"Number of players required to trigger culling.\n" +
				"0 = always cull from the configured direction.\n" +
				"> 0 = when threshold is reached, cull from the opposite direction.");
		}

		bool showRadius = this.configuration.CrowdCullShowRadius;
		if (ImGui.Checkbox("Show Radius", ref showRadius))
		{
			this.configuration.CrowdCullShowRadius = showRadius;
			this.configuration.Save();
		}

		if (this.configuration.CrowdCullShowRadius)
		{
			this.DrawCrowdCullRadiusOverlay();
		}

		ImGui.Unindent();
	}

	private void DrawCrowdCullRadiusOverlay()
	{
		if (Service.ObjectTable.LocalPlayer is not { } localPlayer) return;

		Vector3 center = localPlayer.Position;
		float radius = this.configuration.CrowdCullRadius;
		const int segments = 64;
		const float thickness = 2f;
		uint color = ImGui.GetColorU32(new Vector4(1f, 1f, 0f, 0.8f));

		ImDrawListPtr drawList = ImGui.GetBackgroundDrawList();

		Vector2? prevScreen = null;

		for (int i = 0; i <= segments; ++i)
		{
			float angle = 2f * MathF.PI * i / segments;
			Vector3 worldPoint = new Vector3(
				center.X + (radius * MathF.Cos(angle)),
				center.Y,
				center.Z + (radius * MathF.Sin(angle)));

			if (Service.GameGui.WorldToScreen(worldPoint, out Vector2 screenPoint))
			{
				if (prevScreen.HasValue)
				{
					drawList.AddLine(prevScreen.Value, screenPoint, color, thickness);
				}

				prevScreen = screenPoint;
			}
			else
			{
				prevScreen = null;
			}
		}
	}
}
