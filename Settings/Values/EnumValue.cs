using ImGuiNET;

namespace GoBo.Infrastructure.Settings.Values;

public sealed class EnumValue<TEnum> : SettingValue<TEnum> where TEnum : struct, Enum
{
    private static readonly TEnum[] AllValues = Enum.GetValues<TEnum>();
    private static readonly string[] AllNames = Enum.GetNames<TEnum>();

    public EnumValue() : base(default) { }
    public EnumValue(TEnum defaultValue) : base(defaultValue) { }

    public override bool RenderImGui(string label)
    {
        ImGui.Text(label);
        var currentIndex = Array.IndexOf(AllValues, Value);
        if (currentIndex < 0) currentIndex = 0;

        if (ImGui.Combo($"##{label}", ref currentIndex, AllNames, AllNames.Length))
        {
            Value = AllValues[currentIndex];
            return true;
        }
        return false;
    }
}
