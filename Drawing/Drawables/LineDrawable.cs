using System.Numerics;
using ImGuiNET;

namespace GoBo.Infrastructure.Drawing.Drawables;

internal sealed class LineDrawable : Drawable
{
    public Vector2 Start { get; set; }
    public Vector2 End { get; set; }
    public uint Color { get; set; } = 0xFFFFFFFF;
    public float Thickness { get; set; } = 1.0f;

    public override void Draw(Vector2 screenOffset)
    {
        var drawList = ImGui.GetBackgroundDrawList();
        drawList.AddLine(
            Start + screenOffset,
            End + screenOffset,
            Color,
            Thickness);
    }
}