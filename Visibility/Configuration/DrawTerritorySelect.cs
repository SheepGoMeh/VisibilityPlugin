using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ImGuiNET;

namespace Visibility.Configuration;

public partial class VisibilityConfiguration
{
	[NonSerialized] private bool comboNewOpen;

	/// <summary>
	/// Creates a ComboBox with a simple text filter
	/// </summary>
	/// <param name="label">Widget label</param>
	/// <param name="currentItem">Item output</param>
	/// <param name="itemsDictionary">Input dictionary</param>
	/// <param name="textBuffer">Buffer for text input</param>
	/// <param name="maxItems">Maximum amount of items displayed vertically</param>
	/// <param name="searchIcon">Search icon string</param>
	/// <param name="fontPtr">ImGui font pointer for search icon</param>
	/// <returns></returns>
	private bool ComboWithFilter(
		string label,
		ref ushort currentItem,
		Dictionary<ushort, string> itemsDictionary,
		byte[] textBuffer,
		uint maxItems = 5,
		string searchIcon = "",
		ImFontPtr? fontPtr = null)
	{
		var previewValue = itemsDictionary.ContainsKey(currentItem) ? itemsDictionary[currentItem] : string.Empty;
		
		Dictionary<ushort, string> items = new();

		foreach (var (key, name) in itemsDictionary)
		{
			if (name.Contains(
				    Encoding.UTF8.GetString(textBuffer),
				    StringComparison.InvariantCultureIgnoreCase))
			{
				items[key] = name;
			}
		}

		var valueChanged = false;

		if (!ImGui.BeginCombo(label, previewValue))
		{
			if (textBuffer[0] != 0)
			{
				Array.Clear(textBuffer, 0, textBuffer.Length);
			}

			this.comboNewOpen = false;
			return valueChanged;
		}

		ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(240, 240, 240, 255));
		ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 0, 0, 255));
		ImGui.PushItemWidth(ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X);

		if (!this.comboNewOpen)
		{
			ImGui.SetKeyboardFocusHere();
			this.comboNewOpen = true;
		}

		ImGui.InputText("##ComboWithFilter_inputText", textBuffer, (uint)textBuffer.Length);

		if (fontPtr.HasValue)
		{
			ImGui.PushFont(fontPtr.Value);
		}

		if (string.IsNullOrEmpty(searchIcon) == false)
		{
			var iconSize = ImGui.CalcTextSize(searchIcon, true);
			ImGui.SameLine();
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() - iconSize.X - (ImGui.GetStyle().ItemInnerSpacing.X * 3));
			ImGui.Text(searchIcon);
		}

		if (fontPtr.HasValue)
		{
			ImGui.PopFont();
		}

		ImGui.PopStyleColor(2);

		if (ImGui.BeginListBox(
			    "##ComboWithFilter_itemList",
			    new Vector2(
				    0,
				    (ImGui.GetTextLineHeightWithSpacing() * maxItems) + (ImGui.GetStyle().FramePadding.Y * 2.0f))))
		{
			foreach (var (key, name) in items)
			{
				ImGui.PushID(key);
				var itemSelected = key == currentItem;
				if (ImGui.Selectable(name, itemSelected))
				{
					valueChanged = true;
					currentItem = key;
					ImGui.CloseCurrentPopup();
				}

				if (itemSelected)
				{
					ImGui.SetItemDefaultFocus();
				}

				ImGui.PopID();
			}

			ImGui.EndListBox();
		}

		ImGui.PopItemWidth();
		ImGui.EndCombo();

		if (!valueChanged)
		{
			return valueChanged;
		}

		this.comboNewOpen = false;
		Array.Clear(textBuffer, 0, textBuffer.Length);

		return valueChanged;
	}
}