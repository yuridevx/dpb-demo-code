using System.Numerics;
using System.Text.Json;
using ImGuiNET;

namespace GoBo.Infrastructure.Settings.Values;

public sealed class Vector2Value : SettingValue<Vector2>
{
    public float Min { get; set; } = float.MinValue;
    public float Max { get; set; } = float.MaxValue;
    public float Speed { get; set; } = 1f;
    public string Format { get; set; } = "%.0f";

    public Vector2Value() : base(Vector2.Zero) { }
    public Vector2Value(Vector2 defaultValue) : base(defaultValue) { }

    public override void WriteJson(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", Value.X);
        writer.WriteNumber("y", Value.Y);
        writer.WriteEndObject();
    }

    public override void ReadJson(JsonElement element, JsonSerializerOptions options)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return;

        var x = element.TryGetProperty("x", out var xEl) ? xEl.GetSingle() : DefaultValue.X;
        var y = element.TryGetProperty("y", out var yEl) ? yEl.GetSingle() : DefaultValue.Y;
        Value = new Vector2(x, y);
    }

    public override bool RenderImGui(string label)
    {
        ImGui.Text(label);
        var value = Value;
        if (ImGui.DragFloat2($"##{label}", ref value, Speed, Min, Max, Format))
        {
            Value = value;
            return true;
        }
        return false;
    }
}
