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
					this.VoidList.Remove(itemToRemove);
					this.Save();

					this.sortedContainer[0] = SortContainer(
						this.VoidList,
						this.sortKeySelector[0],
						this.sortAscending[0],
						out this.sortKeySelector[0]);

					if (itemToRemove.ObjectId > 0)
					{
						VisibilityPlugin.Instance.RemoveChecked(itemToRemove.ObjectId);
						VisibilityPlugin.Instance.ShowPlayer(itemToRemove.ObjectId);
					}
					else
					{
						VisibilityPlugin.Instance.RemoveChecked(itemToRemove.Name);
						VisibilityPlugin.Instance.ShowPlayer(itemToRemove.Name);
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
}