using System.Text.Json;

namespace GoBo.Infrastructure.Settings;

/// <summary>
///     Base class for typed setting value wrappers.
///     Provides change notification, default values, and implicit conversion.
/// </summary>
public abstract class SettingValue<T> : ISettingValue
{
    private T _value;

    public event Action? Changed;

    protected SettingValue(T defaultValue)
    {
        _value = defaultValue;
        DefaultValue = defaultValue;
    }

    public T DefaultValue { get; }

    public T Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value))
                return;
            _value = value;
            Changed?.Invoke();
        }
    }

    public static implicit operator T(SettingValue<T> setting) => setting.Value;

    public abstract bool RenderImGui(string label);

    public void ResetToDefault() => Value = DefaultValue;

    public virtual void WriteJson(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, _value, options);
    }

    public virtual void ReadJson(JsonElement element, JsonSerializerOptions options)
    {
        // Preserve default for null/undefined elements
        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return;

        var value = JsonSerializer.Deserialize<T>(element.GetRawText(), options);
        if (value is not null)
            Value = value;
    }
}
