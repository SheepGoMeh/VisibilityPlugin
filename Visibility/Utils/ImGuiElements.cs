using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using Dalamud.Bindings.ImGui;

namespace Visibility.Utils;

public static class ImGuiElements
{
	public static bool Checkbox(bool value, string name)
	{
		if (!ImGui.Checkbox($"###{name}", ref value))
		{
			return false;
		}

		Action<bool, bool, bool>? onValueChanged =
			VisibilityPlugin.Instance.Configuration.SettingsHandler.GetAction(name);

		if (onValueChanged == null)
		{
			return false;
		}

		onValueChanged(value, false, true);
		VisibilityPlugin.Instance.Configuration.Save();
		return true;
	}

	public static bool CenteredCheckbox(bool value, string name)
	{
		ImGui.SetCursorPosX(
			ImGui.GetCursorPosX() +
			((ImGui.GetColumnWidth() + (2 * ImGui.GetStyle().FramePadding.X)) / 2) -
			(2 * ImGui.GetStyle().ItemSpacing.X) - (2 * ImGui.GetStyle().CellPadding.X));

		return Checkbox(value, name);
	}

	/// <summary>
	/// Creates a ComboBox with a simple text filter
	/// </summary>
	/// <param name="label">Widget label</param>
	/// <param name="currentItem">Item output</param>
	/// <param name="comboNewOpen">Boolean tracking if a new combo windows is open</param>
	/// <param name="itemsDictionary">Input dictionary</param>
	/// <param name="textBuffer">Buffer for text input</param>
	/// <param name="maxItems">Maximum amount of items displayed vertically</param>
	/// <param name="searchIcon">Search icon string</param>
	/// <param name="fontPtr">ImGui font pointer for search icon</param>
	/// <returns></returns>
	public static bool ComboWithFilter(
		string label,
		ref ushort currentItem,
		ref bool comboNewOpen,
		Dictionary<ushort, string> itemsDictionary,
		byte[] textBuffer,
		uint maxItems = 5,
		string searchIcon = "",
		ImFontPtr? fontPtr = null)
	{
		string previewValue = itemsDictionary.TryGetValue(currentItem, out string? value) ? value : string.Empty;

		Dictionary<ushort, string> items = new();

		foreach ((ushort key, string? name) in itemsDictionary)
		{
			if (name.Contains(
				    Encoding.UTF8.GetString(textBuffer),
				    StringComparison.InvariantCultureIgnoreCase))
			{
				items[key] = name;
			}
		}

		bool valueChanged = false;

		if (!ImGui.BeginCombo(label, previewValue))
		{
			if (textBuffer[0] != 0)
			{
				Array.Clear(textBuffer, 0, textBuffer.Length);
			}

			comboNewOpen = false;
			return valueChanged;
		}

		ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(240, 240, 240, 255));
		ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 0, 0, 255));
		ImGui.PushItemWidth(ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X);

		if (!comboNewOpen)
		{
			ImGui.SetKeyboardFocusHere();
			comboNewOpen = true;
		}

		ImGui.InputText("##ComboWithFilter_inputText", textBuffer);

		if (fontPtr.HasValue)
		{
			ImGui.PushFont(fontPtr.Value);
		}

		if (string.IsNullOrEmpty(searchIcon) == false)
		{
			Vector2 iconSize = ImGui.CalcTextSize(searchIcon, true);
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
			foreach ((ushort key, string? name) in items)
			{
				ImGui.PushID(key);
				bool itemSelected = key == currentItem;
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

		comboNewOpen = false;
		Array.Clear(textBuffer, 0, textBuffer.Length);

		return valueChanged;
	}
}
