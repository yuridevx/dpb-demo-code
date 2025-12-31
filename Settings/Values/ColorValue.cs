using System.Numerics;
using System.Text.Json;
using ImGuiNET;

namespace GoBo.Infrastructure.Settings.Values;

public sealed class ColorValue : SettingValue<Vector4>
{
    public ColorValue() : base(Vector4.One) { }
    public ColorValue(Vector4 defaultValue) : base(defaultValue) { }

    public ColorValue(string hex) : base(ParseHex(hex)) { }

    private static Vector4 ParseHex(string hex)
    {
        var span = hex.AsSpan();
        if (span[0] == '#') span = span[1..];

        var hasAlpha = span.Length == 8;
        var offset = hasAlpha ? 2 : 0;

        var a = hasAlpha ? int.Parse(span[..2], System.Globalization.NumberStyles.HexNumber) / 255f : 1f;
        var r = int.Parse(span.Slice(offset, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
        var g = int.Parse(span.Slice(offset + 2, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
        var b = int.Parse(span.Slice(offset + 4, 2), System.Globalization.NumberStyles.HexNumber) / 255f;

        return new Vector4(r, g, b, a);
    }

    private string ToHex()
    {
        var v = Value;
        var a = (int)(v.W * 255);
        var r = (int)(v.X * 255);
        var g = (int)(v.Y * 255);
        var b = (int)(v.Z * 255);
        return a == 255
            ? $"#{r:X2}{g:X2}{b:X2}"
            : $"#{a:X2}{r:X2}{g:X2}{b:X2}";
    }

    public override void WriteJson(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WriteStringValue(ToHex());
    }

    public override void ReadJson(JsonElement element, JsonSerializerOptions options)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            var hex = element.GetString();
            if (!string.IsNullOrEmpty(hex))
            {
                Value = ParseHex(hex);
                return;
            }
        }
        // Fallback: keep default value (don't override with zeros)
    }

    public override bool RenderImGui(string label)
    {
        ImGui.Text(label);
        var value = Value;
        if (ImGui.ColorEdit4($"##{label}", ref value))
        {
            Value = value;
            return true;
        }
        return false;
    }

    public static implicit operator uint(ColorValue color)
    {
        var v = color.Value;
        var a = (uint)(v.W * 255) & 0xFF;
        var r = (uint)(v.X * 255) & 0xFF;
        var g = (uint)(v.Y * 255) & 0xFF;
        var b = (uint)(v.Z * 255) & 0xFF;
        return (a << 24) | (r << 16) | (g << 8) | b;
    }
}
