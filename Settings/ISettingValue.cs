using System.Text.Json;

namespace GoBo.Infrastructure.Settings;

/// <summary>
///     Interface for all setting value wrappers.
///     Provides change notification, UI rendering, and JSON serialization.
/// </summary>
public interface ISettingValue
{
    /// <summary>Raised when the value changes.</summary>
    event Action? Changed;

    /// <summary>Renders ImGui editor for this value.</summary>
    bool RenderImGui(string label);

    /// <summary>Resets the value to its default.</summary>
    void ResetToDefault();

    /// <summary>Serializes the value to JSON.</summary>
    void WriteJson(Utf8JsonWriter writer, JsonSerializerOptions options);

    /// <summary>Deserializes the value from JSON.</summary>
    void ReadJson(JsonElement element, JsonSerializerOptions options);
}
