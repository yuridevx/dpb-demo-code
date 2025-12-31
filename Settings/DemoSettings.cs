using System.Numerics;
using GoBo.Infrastructure.Modules;
using GoBo.Infrastructure.Settings.Values;

namespace GoBo.Infrastructure.Settings;

/// <summary>
///     Demo settings showcasing all available value types.
/// </summary>
[Module]
public sealed class DemoSettings : Settings
{
    // SliderIntValue - SliderInt
    public SliderIntValue Count = new(10) { Min = 0, Max = 100 };

    // SliderIntValue - SliderInt
    public SliderIntValue Volume = new(50) { Min = 0, Max = 100 };

    // InputIntValue - InputInt
    public InputIntValue Port = new(8080);

    // SliderFloatValue - SliderFloat
    public SliderFloatValue Scale = new(1.5f) { Min = 0.1f, Max = 5f };

    // SliderFloatValue - SliderFloat
    public SliderFloatValue Opacity = new(0.8f) { Min = 0f, Max = 1f };

    // InputFloatValue - InputFloat
    public InputFloatValue Precision = new(3.14159f);

    // BoolValue - Checkbox
    public BoolValue Enabled = new(true);

    // EnumValue - Dropdown
    public EnumValue<DemoMode> Mode = new(DemoMode.Normal);

    // Vector2Value - DragFloat2
    public Vector2Value Position = new(new Vector2(100, 200)) { Min = 0, Max = 1000 };

    // ColorValue - ColorEdit4
    public ColorValue HighlightColor = new(new Vector4(1f, 1f, 0f, 0.8f));

    // StringValue - InputText
    public StringValue Name = new("Player");
}

public enum DemoMode { Normal, Fast, Slow }
