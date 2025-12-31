using ImGuiNET;

namespace GoBo.Infrastructure.Settings.Values;

public sealed class SliderFloatValue : SettingValue<float>
{
    public float Min { get; set; } = 0f;
    public float Max { get; set; } = 1f;
    public string Format { get; set; } = "%.2f";

    public SliderFloatValue() : base(0f) { }
    public SliderFloatValue(float defaultValue) : base(defaultValue) { }

    public override bool RenderImGui(string label)
    {
        ImGui.Text(label);
        var sliderValue = Math.Clamp(Value, Min, Max);
        if (ImGui.SliderFloat($"##{label}", ref sliderValue, Min, Max, Format))
        {
            Value = sliderValue;
            return true;
        }
        return false;
    }
}
