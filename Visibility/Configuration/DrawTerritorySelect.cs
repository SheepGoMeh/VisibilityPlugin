using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Interface;
using ImGuiNET;

namespace Visibility.Configuration;

public partial class VisibilityConfiguration
{
	private static void RenderArrow(ImDrawListPtr drawList, Vector2 pos, uint col, ImGuiDir dir, float scale = 1.0f)
	{
		var h = ImGui.GetFontSize();
		var r = h * 0.4f * scale;
		var center = pos + new Vector2(h * 0.5f, h * 0.5f * scale);

		Vector2 a, b, c;
		switch (dir)
		{
			case ImGuiDir.Left:
			case ImGuiDir.Right:
				if (dir == ImGuiDir.Left)
				{
					r = -r;
				}

				a = new Vector2(0.75f, 0.0f) * r;
				b = new Vector2(-0.75f, 0.866f) * r;
				c = new Vector2(-0.75f, -0.866f) * r;
				break;
			case ImGuiDir.Up:
			case ImGuiDir.Down:
				if (dir == ImGuiDir.Up)
				{
					r = -r;
				}

				a = new Vector2(0.0f, 0.75f) * r;
				b = new Vector2(-0.866f, -0.75f) * r;
				c = new Vector2(0.866f, -0.75f) * r;
				break;
			case ImGuiDir.None:
			case ImGuiDir.COUNT:
			default:
				throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
		}

		drawList.AddTriangleFilled(center + a, center + b, center + c, col);
	}

	private bool ComboWithFilter(string label, ref ushort currentItem, Dictionary<ushort, string> itemsDictionary)
	{
		var previewValue = itemsDictionary.ContainsKey(currentItem) ? itemsDictionary[currentItem] : string.Empty;
		var fontSize = ImGui.GetFontSize();
		var expectedWidth = ImGui.CalcItemWidth();
		var itemMin = ImGui.GetItemRectMin();
		var sz = ImGui.GetFrameHeight();
		var size = new Vector2(sz);
		var cursorPos = ImGui.GetCursorScreenPos();
		var pos = cursorPos + new Vector2(expectedWidth - sz, 0);

		var buttonTextAlignX = ImGui.GetStyle().ButtonTextAlign.X;
		ImGui.GetStyle().ButtonTextAlign.X = 0;
		var isNewOpen = false;

		if (ImGui.Button($"{previewValue}##name_ComboWithFilter_button_{label}", new Vector2(expectedWidth, 0)))
		{
			ImGui.OpenPopup($"##name_popup_{label}");
			isNewOpen = true;
		}

		ImGui.GetStyle().ButtonTextAlign.X = buttonTextAlignX;
		var valueChanged = false;

		RenderArrow(
			ImGui.GetWindowDrawList(),
			pos + new Vector2(Math.Max(0.0f, (size.X - fontSize) * 0.5f), Math.Max(0.0f, (size.Y - fontSize) * 0.5f)),
			ImGui.GetColorU32(ImGuiCol.Text),
			ImGuiDir.Down);

		if (isNewOpen)
		{
			Array.Clear(this.buffer[8], 0, this.buffer[8].Length);
		}

		var itemMax = ImGui.GetItemRectMax();
		ImGui.SetNextWindowPos(new Vector2(cursorPos.X, itemMax.Y));
		var winSize = new Vector2(ImGui.GetItemRectSize().X, 0);
		ImGui.SetNextWindowSize(new Vector2(ImGui.GetItemRectSize().X, 0));
		if (!ImGui.BeginPopup($"##name_popup_{label}"))
		{
			return valueChanged;
		}

		ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(240, 240, 240, 255));
		ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 0, 0, 255));
		ImGui.PushItemWidth(-1.175494351e-38F);

		if (isNewOpen)
		{
			ImGui.SetKeyboardFocusHere();
		}

		ImGui.InputText("##ComboWithFilter_inputText", this.buffer[8], (uint)this.buffer[8].Length);

		ImGui.PushFont(UiBuilder.IconFont);

		var iconString = FontAwesomeIcon.Search.ToIconString();
		var iconSize = ImGui.CalcTextSize(iconString, true);
		ImGui.GetWindowDrawList().AddText(
			new Vector2(
				ImGui.GetItemRectMax().X - iconSize.X - (ImGui.GetStyle().ItemInnerSpacing.X * 2),
				cursorPos.Y + ImGui.GetStyle().FramePadding.Y + (fontSize * 0.3f)),
			ImGui.GetColorU32(ImGuiCol.Text),
			iconString);

		ImGui.PopFont();

		ImGui.PopStyleColor(2);

		Dictionary<ushort, string> items = new();

		foreach (var (key, name) in itemsDictionary)
		{
			if (name.Contains(
				    Encoding.UTF8.GetString(this.buffer[8]),
				    StringComparison.InvariantCultureIgnoreCase))
			{
				items[key] = name;
			}
		}

		if (ImGui.BeginListBox(
			    "##ComboWithFilter_itemList",
			    winSize with { X = winSize.X - ImGui.GetStyle().ScrollbarSize }))
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
		ImGui.EndPopup();

		return valueChanged;
	}
}