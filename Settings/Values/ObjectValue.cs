using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using DreamPoeBot.Loki.Common;
using ImGuiNET;
using log4net;

namespace GoBo.Infrastructure.Settings.Values;

/// <summary>
///     Value wrapper for complex object types.
///     Provides a JSON editor popup for editing complex structures.
/// </summary>
public sealed class ObjectValue<T> : SettingValue<T> where T : class, new()
{
    private static readonly ILog Log = Logger.GetLoggerInstanceForType();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private bool _popupOpen;
    private string _jsonBuffer = string.Empty;
    private string? _parseError;

    public ObjectValue() : base(new T()) { }
    public ObjectValue(T defaultValue) : base(defaultValue) { }

    public override bool RenderImGui(string label)
    {
        var changed = false;

        ImGui.Text(label);
        if (ImGui.Button($"Edit JSON##{label}_edit"))
        {
            _popupOpen = true;
            _jsonBuffer = SerializeToJson();
            _parseError = null;
            ImGui.OpenPopup($"##{label}_popup");
        }

        // Full-screen popup
        if (_popupOpen)
        {
            changed = RenderJsonPopup(label);
        }

        return changed;
    }

    private bool RenderJsonPopup(string label)
    {
        var changed = false;
        var popupId = $"##{label}_popup";

        // Set popup to cover most of the screen
        var viewport = ImGui.GetMainViewport();
        var popupSize = new Vector2(viewport.Size.X * 0.8f, viewport.Size.Y * 0.8f);
        var popupPos = viewport.Pos + (viewport.Size - popupSize) * 0.5f;

        ImGui.SetNextWindowPos(popupPos, ImGuiCond.Appearing);
        ImGui.SetNextWindowSize(popupSize, ImGuiCond.Appearing);

        var flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings;

        if (ImGui.Begin($"Edit {label}##{label}_popup_window", ref _popupOpen, flags))
        {
            // Error display
            if (_parseError != null)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.3f, 0.3f, 1f));
                ImGui.TextWrapped(_parseError);
                ImGui.PopStyleColor();
                ImGui.Separator();
            }

            // JSON editor
            var editorHeight = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing() - ImGui.GetStyle().ItemSpacing.Y;
            ImGui.InputTextMultiline(
                $"##{label}_json",
                ref _jsonBuffer,
                1024 * 64, // 64KB max
                new Vector2(-1, editorHeight),
                ImGuiInputTextFlags.AllowTabInput);

            // Buttons
            if (ImGui.Button("Apply"))
            {
                if (TryParseJson(out var parsed))
                {
                    Value = parsed;
                    changed = true;
                    _popupOpen = false;
                    _parseError = null;
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                _popupOpen = false;
                _parseError = null;
            }

            ImGui.SameLine();
            if (ImGui.Button("Reset to Default"))
            {
                _jsonBuffer = JsonSerializer.Serialize(DefaultValue, JsonOptions);
                _parseError = null;
            }

            ImGui.SameLine();
            if (ImGui.Button("Format"))
            {
                if (TryParseJson(out var parsed))
                {
                    _jsonBuffer = JsonSerializer.Serialize(parsed, JsonOptions);
                    _parseError = null;
                }
            }
        }
        ImGui.End();

        return changed;
    }

    private string SerializeToJson()
    {
        try
        {
            return JsonSerializer.Serialize(Value, JsonOptions);
        }
        catch (Exception ex)
        {
            Log.Error($"[ObjectValue] Failed to serialize: {ex.Message}");
            return "{}";
        }
    }

    private bool TryParseJson(out T result)
    {
        result = default!;
        try
        {
            result = JsonSerializer.Deserialize<T>(_jsonBuffer, JsonOptions) ?? new T();
            _parseError = null;
            return true;
        }
        catch (JsonException ex)
        {
            _parseError = $"JSON Error: {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            _parseError = $"Error: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    ///     Notifies that the value has changed.
    ///     Call this after modifying the internal state of the wrapped object.
    /// </summary>
    public void NotifyChanged()
    {
        // Re-assign to trigger change event
        var current = Value;
        // Force the change event by temporarily setting to different instance
        base.Value = current;
    }
}
