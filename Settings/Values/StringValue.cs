using ImGuiNET;

namespace GoBo.Infrastructure.Settings.Values;

public sealed class StringValue : SettingValue<string>
{
    public int MaxLength { get; set; } = 256;

    public StringValue() : base(string.Empty) { }
    public StringValue(string defaultValue) : base(defaultValue) { }

    public override bool RenderImGui(string label)
    {
        ImGui.Text(label);
        var value = Value;
        if (ImGui.InputText($"##{label}", ref value, (uint)MaxLength))
        {
            Value = value;
            return true;
        }
        return false;
    }
}
