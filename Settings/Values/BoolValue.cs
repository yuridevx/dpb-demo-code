using ImGuiNET;

namespace GoBo.Infrastructure.Settings.Values;

public sealed class BoolValue : SettingValue<bool>
{
    public BoolValue() : base(false) { }
    public BoolValue(bool defaultValue) : base(defaultValue) { }

    public override bool RenderImGui(string label)
    {
        var value = Value;
        if (ImGui.Checkbox(label, ref value))
        {
            Value = value;
            return true;
        }
        return false;
    }
}
