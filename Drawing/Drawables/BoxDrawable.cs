using System.Numerics;
using ImGuiNET;

namespace GoBo.Infrastructure.Drawing.Drawables;

internal sealed class BoxDrawable : Drawable
{
    public Vector2 TopLeft { get; set; }
    public Vector2 BottomRight { get; set; }
    public uint Color { get; set; } = 0xFFFFFFFF;
    public bool Filled { get; set; } = false;
    public float Thickness { get; set; } = 1.0f;

    public override void Draw(Vector2 screenOffset)
    {
        var drawList = ImGui.GetBackgroundDrawList();
        var tl = TopLeft + screenOffset;
        var br = BottomRight + screenOffset;

        if (Filled)
            drawList.AddRectFilled(tl, br, Color);
        else
            drawList.AddRect(tl, br, Color, 0, ImDrawFlags.None, Thickness);
    }
}