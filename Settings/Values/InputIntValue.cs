using ImGuiNET;

namespace GoBo.Infrastructure.Settings.Values;

public sealed class InputIntValue : SettingValue<int>
{
    public InputIntValue() : base(0) { }
    public InputIntValue(int defaultValue) : base(defaultValue) { }

    public override bool RenderImGui(string label)
    {
        ImGui.Text(label);
        var inputValue = Value;
        if (ImGui.InputInt($"##{label}", ref inputValue))
        {
            Value = inputValue;
            return true;
        }
        return false;
    }
}
