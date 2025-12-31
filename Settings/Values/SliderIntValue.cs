using ImGuiNET;

namespace GoBo.Infrastructure.Settings.Values;

public sealed class SliderIntValue : SettingValue<int>
{
    public int Min { get; set; } = 0;
    public int Max { get; set; } = 100;

    public SliderIntValue() : base(0) { }
    public SliderIntValue(int defaultValue) : base(defaultValue) { }

    public override bool RenderImGui(string label)
    {
        ImGui.Text(label);
        var sliderValue = Math.Clamp(Value, Min, Max);
        if (ImGui.SliderInt($"##{label}", ref sliderValue, Min, Max))
        {
            Value = sliderValue;
            return true;
        }
        return false;
    }
}
