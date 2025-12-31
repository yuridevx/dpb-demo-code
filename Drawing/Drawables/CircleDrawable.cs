using System.Numerics;
using ImGuiNET;

namespace GoBo.Infrastructure.Drawing.Drawables;

internal sealed class CircleDrawable : Drawable
{
    public Vector2 Center { get; set; }
    public float Radius { get; set; }
    public uint Color { get; set; } = 0xFFFFFFFF;
    public bool Filled { get; set; } = false;
    public float Thickness { get; set; } = 1.0f;

    public override void Draw(Vector2 screenOffset)
    {
        var drawList = ImGui.GetBackgroundDrawList();
        var center = Center + screenOffset;

        if (Filled)
            drawList.AddCircleFilled(center, Radius, Color);
        else
            drawList.AddCircle(center, Radius, Color, 0, Thickness);
    }
}