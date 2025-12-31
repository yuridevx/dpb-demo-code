using System.Numerics;
using ImGuiNET;

namespace GoBo.Infrastructure.Drawing.Drawables;

internal sealed class TextDrawable : Drawable
{
    public string Text { get; set; }
    public Vector2 Position { get; set; }
    public uint Color { get; set; } = 0xFFFFFFFF;

    public Vector2 GetTextSize()
    {
        if (string.IsNullOrEmpty(Text))
            return Vector2.Zero;
        return ImGui.CalcTextSize(Text);
    }

    public override void Draw(Vector2 screenOffset)
    {
        var finalPos = Position + screenOffset;

        var drawList = ImGui.GetBackgroundDrawList();
        drawList.AddText(finalPos, Color, Text);
    }
}