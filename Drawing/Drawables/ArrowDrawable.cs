using System.Numerics;
using ImGuiNET;

namespace GoBo.Infrastructure.Drawing.Drawables;

internal sealed class ArrowDrawable : Drawable
{
    public Vector2 Start { get; set; }
    public Vector2 End { get; set; }
    public uint Color { get; set; } = 0xFFFFFFFF;
    public float Thickness { get; set; } = 1.0f;
    public float HeadLength { get; set; } = 10f;
    public float HeadAngle { get; set; } = 0.5f; // ~30 degrees in radians

    public override void Draw(Vector2 screenOffset)
    {
        var drawList = ImGui.GetBackgroundDrawList();
        var start = Start + screenOffset;
        var end = End + screenOffset;

        // Draw main line
        drawList.AddLine(start, end, Color, Thickness);

        // Calculate arrowhead
        var direction = end - start;
        var length = direction.Length();
        if (length < 0.001f)
            return;

        direction /= length; // normalize

        // Arrowhead lines
        var perpendicular = new Vector2(-direction.Y, direction.X);
        var headBase = end - direction * HeadLength;

        var headLeft = headBase + perpendicular * HeadLength * MathF.Tan(HeadAngle);
        var headRight = headBase - perpendicular * HeadLength * MathF.Tan(HeadAngle);

        drawList.AddLine(end, headLeft, Color, Thickness);
        drawList.AddLine(end, headRight, Color, Thickness);
    }
}
