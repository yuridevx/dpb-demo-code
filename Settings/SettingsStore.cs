using System.IO;
using System.Text;
using System.Text.Json;
using DreamPoeBot.Loki.Common;
using GoBo.Infrastructure.Lifecycle;
using GoBo.Infrastructure.Modules;
using log4net;

namespace GoBo.Infrastructure.Settings;

/// <summary>
///     Persists settings to disk when they change.
///     Subscribes to all Settings.Changed events during Initialize.
///     JSON serialization is centralized here using BoxedValue/ValueType.
/// </summary>
[Module(Priority = Priority.Core)]
public sealed class SettingsStore(IReadOnlyList<Settings> allSettings)
    : IModule
{
    private static readonly ILog Log = Logger.GetLoggerInstanceForType();

    public void Initialize()
    {
        // Ensure directory exists
        if (!Directory.Exists(SettingsFactory.BasePath))
            Directory.CreateDirectory(SettingsFactory.BasePath);

        // Subscribe to all settings change events
        foreach (var settings in allSettings)
        {
            settings.Changed += () => Save(settings);
        }

        // Save all settings (persist defaults for new fields)
        SaveAll();
    }

    public void Deinitialize() => SaveAll();

    private void Save(Settings settings)
    {
        try
        {
            var path = SettingsFactory.GetPath(settings.GetType());

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

            writer.WriteStartObject();

            foreach (var field in SettingsFactory.GetSettingValueFields(settings.GetType()))
            {
                var jsonName = JsonNamingPolicy.CamelCase.ConvertName(field.Name);
                var settingValue = (ISettingValue)field.GetValue(settings)!;

                writer.WritePropertyName(jsonName);
                settingValue.WriteJson(writer, SettingsFactory.JsonOptions);
            }

            writer.WriteEndObject();
            writer.Flush();

            var json = Encoding.UTF8.GetString(stream.ToArray());
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Log.Error($"[Settings] Failed to save: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void SaveAll()
    {
        foreach (var settings in allSettings)
            Save(settings);
    }
}
