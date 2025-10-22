using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

using Visibility.Configuration;
using Visibility.Utils;
using Visibility.Void;

namespace Visibility.Windows;

public class VoidPatternList: Window
{
	public VoidPatternList(): base(
		$"{VisibilityPlugin.Instance.Name}: ", 0, true)
	{
		this.WindowName += VisibilityPlugin.Instance.PluginLocalization.PatternListName;
		this.Size = new Vector2(700, 500);
		this.SizeCondition = ImGuiCond.FirstUseEver;
	}

	private bool sortAscending;
	private IEnumerable<VoidPattern>? sortedContainer;
	private Func<VoidPattern, object>? sortKeySelector;

	private readonly byte[][] buffer = { new byte[128], new byte[128] };

	public override void Draw()
	{
		if (!ImGui.BeginTable(
			    "PatternVoidListTable",
			    5,
			    ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable))
		{
			return;
		}

		ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.PatternName);
		ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.PatternDescription);
		ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.PatternOffworld);
		ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.OptionEnable);
		ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.ColumnAction, ImGuiTableColumnFlags.NoSort);
		ImGui.TableSetupScrollFreeze(0, 1);
		ImGui.TableHeadersRow();

		VoidPattern? itemToRemove = null;

		VisibilityConfiguration configuration = VisibilityPlugin.Instance.Configuration;

		List<VoidPattern> container = configuration.VoidPatterns;
		this.sortedContainer ??= container;

		ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();

		if (sortSpecs.SpecsDirty)
		{
			this.sortAscending = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending;

			this.sortedContainer = sortSpecs.Specs.ColumnIndex switch
			{
				0 => SortContainer(
					container,
					x => x.Pattern,
					this.sortAscending,
					out this.sortKeySelector),
				1 => SortContainer(
					container,
					x => x.Description,
					this.sortAscending,
					out this.sortKeySelector),
				2 => SortContainer(
					container,
					x => x.Enabled,
					this.sortAscending,
					out this.sortKeySelector),
				_ => this.sortedContainer
			};

			sortSpecs.SpecsDirty = false;
		}

		foreach (VoidPattern item in this.sortedContainer)
		{
			ImGui.TableNextColumn();
			ImGui.TextUnformatted(item.Pattern);
			ImGui.TableNextColumn();
			ImGui.TextUnformatted(item.Description);
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				item.Offworld,
				$"VoidPattern##{item.Id}##Offworld",
				((value, _, _) =>
				{
					item.Offworld = value;
					configuration.Save();
					// Refresh and recalculate visibility
					VisibilityPlugin.Instance.FrameworkHandler.ClearRegexCache();
				}));
			ImGui.TableNextColumn();
			ImGuiElements.CenteredCheckbox(
				item.Enabled,
				$"VoidPattern##{item.Id}##Enabled",
				((value, _, _) =>
				{
					item.Enabled = value;
					configuration.Save();
					// Refresh and recalculate visibility
					VisibilityPlugin.Instance.FrameworkHandler.ClearRegexCache();
				}));
			ImGui.TableNextColumn();

			if (ImGui.Button(
				    $"{VisibilityPlugin.Instance.PluginLocalization.OptionRemovePlayer}##{item.Id}"))
			{
				itemToRemove = item;
			}

			ImGui.TableNextRow();
		}

		if (itemToRemove != null)
		{
			container.Remove(itemToRemove);
			configuration.Save();

			if (this.sortKeySelector != null)
			{
				this.sortedContainer = SortContainer(
					container,
					this.sortKeySelector,
					this.sortAscending,
					out this.sortKeySelector);
			}

			// Refresh and recalculate visibility
			VisibilityPlugin.Instance.FrameworkHandler.ClearRegexCache();
		}

		bool enabled = true;
		bool offworld = true;

		ImGui.TableNextColumn();
		ImGui.InputText(
			"###voidPattern",
			this.buffer[0]);
		ImGui.TableNextColumn();
		ImGui.InputText(
			"###voidPatternReason",
			this.buffer[1]);
		ImGui.TableNextColumn();
		ImGuiElements.CenteredCheckbox(
			offworld,
			"###voidPatternOffworld");
		ImGui.TableNextColumn();
		ImGuiElements.CenteredCheckbox(
			enabled,
			"###voidPatternEnabled");
		ImGui.TableNextColumn();

		if (ImGui.Button(VisibilityPlugin.Instance.PluginLocalization.ActionAddPattern))
		{
			VoidPattern pattern;
			try
			{
				pattern = new(
					Guid.NewGuid().ToString(),
					0,
					this.buffer[0].ByteToString(),
					this.buffer[1].ByteToString(),
					enabled);
			}
			catch (Exception ex)
			{
				Service.PluginLog.Error($"Error registering pattern {this.buffer[0].ByteToString()}:\n{ex}");
				return;
			}

			configuration.VoidPatterns.Add(pattern);
			configuration.Save();

			VisibilityPlugin.Instance.FrameworkHandler.ClearRegexCache();

			foreach (byte[] item in this.buffer)
			{
				Array.Clear(item, 0, item.Length);
			}

			if (this.sortKeySelector != null)
			{
				this.sortedContainer = SortContainer(
					container,
					this.sortKeySelector,
					this.sortAscending,
					out this.sortKeySelector);
			}
		}

		ImGui.EndTable();
	}

	private static IEnumerable<VoidPattern> SortContainer(
		IEnumerable<VoidPattern> container,
		Func<VoidPattern, object> keySelector,
		bool isAscending,
		out Func<VoidPattern, object> keySelectorOut)
	{
		keySelectorOut = keySelector;
		return isAscending ? container.OrderBy(keySelector) : container.OrderByDescending(keySelector);
	}
}
