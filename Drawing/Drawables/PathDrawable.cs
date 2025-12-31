using System.Numerics;
using ImGuiNET;

namespace GoBo.Infrastructure.Drawing.Drawables;

internal sealed class PathDrawable : Drawable
{
    public List<Vector2> Points { get; set; } = [];
    public uint StrokeColor { get; set; } = 0xFFFFFFFF;
    public uint FillColor { get; set; } = 0x80FFFFFF;
    public float StrokeThickness { get; set; } = 1.0f;
    public bool Filled { get; set; } = false;

    public override void Draw(Vector2 screenOffset)
    {
        if (Points.Count < 3) return;

        var screenPoints = Points
            .Select(v => v + screenOffset)
            .ToArray();

        var drawList = ImGui.GetBackgroundDrawList();

        if (Filled)
            drawList.AddConvexPolyFilled(ref screenPoints[0], screenPoints.Length, FillColor);

        drawList.AddPolyline(ref screenPoints[0], screenPoints.Length, StrokeColor, ImDrawFlags.Closed,
            StrokeThickness);
    }
}