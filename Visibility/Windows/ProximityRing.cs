using System;
using System.Numerics;

using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Windowing;

using ImGuiNET;

namespace Visibility.Windows;

public class ProximityRing: Window
{
	public ProximityRing(): base($"ProximityRing##{VisibilityPlugin.Instance.Name}",
		ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoTitleBar |
		ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground, true)
	{
		this.IsOpen = true;
		this.Position = Vector2.Zero;
		this.Size = ImGui.GetIO().DisplaySize;
	}

	public override bool DrawConditions() => (VisibilityPlugin.Instance.Configuration.PreviewProximityRadius &&
	                                          VisibilityPlugin.Instance.ConfigurationWindow.IsOpen &&
	                                          Service.ClientState.LocalPlayer != null);

	public override void Draw()
	{
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		PlayerCharacter localPlayer = Service.ClientState.LocalPlayer!;

		const int numSegments = 32;
		float radius = VisibilityPlugin.Instance.Configuration.ProximityRadius;

		for (int i = 0; i < numSegments * 2; i++)
		{
			Service.GameGui.WorldToScreen(
				new Vector3(
					localPlayer.Position.X + (radius * (float)Math.Cos(Math.PI / numSegments * i)),
					localPlayer.Position.Y,
					localPlayer.Position.Z + (radius * (float)Math.Sin(Math.PI / numSegments * i))),
				out Vector2 pos);

			drawList.PathLineTo(pos);
		}

		drawList.PathStroke(0xFFFFFFFF, ImDrawFlags.Closed, 1);
	}
}
