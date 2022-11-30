using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ImGuiNET;
using Visibility.Void;

namespace Visibility.Configuration;

public partial class VisibilityConfiguration
{
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

					if (itemToRemove.ObjectId > 0)
					{
						VisibilityPlugin.Instance.RemoveChecked(itemToRemove.ObjectId);
					}
					else
					{
						VisibilityPlugin.Instance.RemoveChecked(itemToRemove.Name);
					}
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