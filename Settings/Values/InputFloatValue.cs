using ImGuiNET;

namespace GoBo.Infrastructure.Settings.Values;

public sealed class InputFloatValue : SettingValue<float>
{
    public string Format { get; set; } = "%.2f";

    public InputFloatValue() : base(0f) { }
    public InputFloatValue(float defaultValue) : base(defaultValue) { }

    public override bool RenderImGui(string label)
    {
        ImGui.Text(label);
        var inputValue = Value;
        if (ImGui.InputFloat($"##{label}", ref inputValue, 0, 0, Format))
        {
            Value = inputValue;
            return true;
        }
        return false;
    }
}
