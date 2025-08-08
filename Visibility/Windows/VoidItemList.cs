using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Windowing;

using Dalamud.Bindings.ImGui;

using Visibility.Configuration;
using Visibility.Void;

namespace Visibility.Windows;

public class VoidItemList: Window
{
	public VoidItemList(bool isWhitelist = false): base($"{VisibilityPlugin.Instance.Name}: ", 0, true)
	{
		this.WindowName += isWhitelist
			? VisibilityPlugin.Instance.PluginLocalization.WhitelistName
			: VisibilityPlugin.Instance.PluginLocalization.VoidListName;

		this.isWhitelist = isWhitelist;
		this.Size = new Vector2(700, 500);
		this.SizeCondition = ImGuiCond.FirstUseEver;
	}

	private bool sortAscending;
	private IEnumerable<VoidItem>? sortedContainer;
	private Func<VoidItem, object>? sortKeySelector;

	private readonly bool isWhitelist;

	private readonly byte[][] buffer = { new byte[16], new byte[16], new byte[128], new byte[128], };

	public override void Draw()
	{
		if (!ImGui.BeginTable(
			    this.isWhitelist ? "WhitelistTable" : "VoidListTable",
			    6,
			    ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable))
		{
			return;
		}

		ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.ColumnFirstname);
		ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.ColumnLastname);
		ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.ColumnWorld);
		ImGui.TableSetupColumn(
			VisibilityPlugin.Instance.PluginLocalization.ColumnDate,
			ImGuiTableColumnFlags.DefaultSort);
		ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.ColumnReason);
		ImGui.TableSetupColumn(VisibilityPlugin.Instance.PluginLocalization.ColumnAction, ImGuiTableColumnFlags.NoSort);
		ImGui.TableSetupScrollFreeze(0, 1);
		ImGui.TableHeadersRow();

		VoidItem? itemToRemove = null;

		VisibilityConfiguration configuration = VisibilityPlugin.Instance.Configuration;

		List<VoidItem> container = this.isWhitelist ? configuration.Whitelist : configuration.VoidList;
		this.sortedContainer ??= container;

		ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();

		if (sortSpecs.SpecsDirty)
		{
			this.sortAscending = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending;

			this.sortedContainer = sortSpecs.Specs.ColumnIndex switch
			{
				0 => SortContainer(
					container,
					x => x.Firstname,
					this.sortAscending,
					out this.sortKeySelector),
				1 => SortContainer(
					container,
					x => x.Lastname,
					this.sortAscending,
					out this.sortKeySelector),
				2 => SortContainer(
					container,
					x => x.HomeworldName,
					this.sortAscending,
					out this.sortKeySelector),
				3 => SortContainer(container, x => x.Time, this.sortAscending, out this.sortKeySelector),
				4 => SortContainer(container, x => x.Reason, this.sortAscending, out this.sortKeySelector),
				_ => this.sortedContainer
			};

			sortSpecs.SpecsDirty = false;
		}

		foreach (VoidItem item in this.sortedContainer)
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

			if (itemToRemove.ObjectId > 0)
			{
				VisibilityPlugin.Instance.RemoveChecked(itemToRemove.ObjectId);

				if (!this.isWhitelist)
				{
					VisibilityPlugin.Instance.ShowPlayer(itemToRemove.ObjectId);
				}
			}
			else
			{
				VisibilityPlugin.Instance.RemoveChecked(itemToRemove.Name);

				if (!this.isWhitelist)
				{
					VisibilityPlugin.Instance.ShowPlayer(itemToRemove.Name);
				}
			}
		}

		bool manual = true;

		if (Service.ClientState.LocalPlayer?.TargetObjectId > 0
		    && Service.ObjectTable
				    .SingleOrDefault(
					    x => x is IPlayerCharacter
					         && x.ObjectKind != ObjectKind.Companion
					         && x.EntityId == Service.ClientState.LocalPlayer
						         ?.TargetObjectId) is
			    IPlayerCharacter actor)
		{
			Array.Clear(this.buffer[0], 0, this.buffer[0].Length);
			Array.Clear(this.buffer[1], 0, this.buffer[1].Length);
			Array.Clear(this.buffer[2], 0, this.buffer[2].Length);

			Encoding.Default.GetBytes(actor.GetFirstname()).CopyTo(this.buffer[0], 0);
			Encoding.Default.GetBytes(actor.GetLastname()).CopyTo(this.buffer[1], 0);
			Encoding.Default.GetBytes(actor.HomeWorld.Value.Name.ToString()).CopyTo(this.buffer[2], 0);

			manual = false;
		}

		ImGui.TableNextColumn();
		ImGui.InputText(
			"###playerFirstName",
			this.buffer[0],
			ImGuiInputTextFlags.CharsNoBlank);
		ImGui.TableNextColumn();
		ImGui.InputText(
			"###playerLastName",
			this.buffer[1],
			ImGuiInputTextFlags.CharsNoBlank);
		ImGui.TableNextColumn();
		ImGui.InputText(
			"###homeworldName",
			this.buffer[2],
			ImGuiInputTextFlags.CharsNoBlank);
		ImGui.TableNextColumn();
		ImGui.TableNextColumn();
		ImGui.InputText("###reason", this.buffer[3]);
		ImGui.TableNextColumn();

		if (ImGui.Button(VisibilityPlugin.Instance.PluginLocalization.OptionAddPlayer))
		{
			if (this.isWhitelist)
			{
				VisibilityPlugin.Instance.CommandManagerHandler.WhitelistPlayer(
					manual ? "WhitelistUIManual" : string.Empty,
					$"{this.buffer[0].ByteToString()} {this.buffer[1].ByteToString()} {this.buffer[2].ByteToString()} {this.buffer[3].ByteToString()}");
			}
			else
			{
				VisibilityPlugin.Instance.CommandManagerHandler.VoidPlayer(
					manual ? "VoidUIManual" : string.Empty,
					$"{this.buffer[0].ByteToString()} {this.buffer[1].ByteToString()} {this.buffer[2].ByteToString()} {this.buffer[3].ByteToString()}");
			}

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
