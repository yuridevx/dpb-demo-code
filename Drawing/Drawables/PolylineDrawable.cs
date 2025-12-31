using System.Numerics;
using ImGuiNET;

namespace GoBo.Infrastructure.Drawing.Drawables;

internal sealed class PolylineDrawable : Drawable
{
    public List<Vector2> Points { get; set; } = [];
    public uint Color { get; set; } = 0xFFFFFFFF;
    public float Thickness { get; set; } = 1.0f;
    public bool Closed { get; set; } = false;

    public override void Draw(Vector2 screenOffset)
    {
        if (Points.Count < 2) return;

        var screenPoints = Points
            .Select(v => v + screenOffset)
            .ToArray();

        var drawList = ImGui.GetBackgroundDrawList();
        var flags = Closed ? ImDrawFlags.Closed : ImDrawFlags.None;
        drawList.AddPolyline(ref screenPoints[0], screenPoints.Length, Color, flags, Thickness);
    }
}