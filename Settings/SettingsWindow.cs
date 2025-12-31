using System.Numerics;
using System.Text;
using DreamPoeBot.Loki.Common;
using GoBo.Infrastructure.Modules;
using GoBo.Infrastructure.Windows;
using ImGuiNET;
using log4net;

namespace GoBo.Infrastructure.Settings;

/// <summary>
///     Window module that renders all Settings classes with tabs.
///     Each settings class becomes a tab, fields are rendered via reflection.
/// </summary>
[Module(Priority = Priority.Render)]
public sealed class SettingsWindow(IReadOnlyList<Settings> allSettings) : IWindowModule
{
    private static readonly ILog Log = Logger.GetLoggerInstanceForType();

    public string Title => "Settings";
    public string Category => "Config";
    public int Order => 0;
    public Vector2 DefaultSize => new(450, 400);

    public void RenderContent()
    {
        if (allSettings.Count == 0)
        {
            ImGui.TextDisabled("No settings to display");
            return;
        }

        // Single settings class - no tabs needed
        if (allSettings.Count == 1)
        {
            RenderSettingsClass(allSettings[0]);
            return;
        }

        // Multiple settings classes - use tabs
        if (ImGui.BeginTabBar("SettingsTabs"))
        {
            foreach (var settings in allSettings)
            {
                var tabName = GetDisplayName(settings.GetType());
                if (ImGui.BeginTabItem(tabName))
                {
                    RenderSettingsClass(settings);
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }
    }

    private void RenderSettingsClass(Settings settings)
    {
        try
        {
            var fields = SettingsFactory.GetSettingValueFields(settings.GetType());

            foreach (var field in fields)
            {
                var settingValue = (ISettingValue)field.GetValue(settings)!;
                var label = FormatFieldName(field.Name);

                ImGui.PushID(field.Name);
                try
                {
                    settingValue.RenderImGui(label);
                }
                catch (Exception ex)
                {
                    Log.Error($"[SettingsWindow] Error rendering {field.Name}: {ex.Message}");
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Error: {field.Name}");
                }
                finally
                {
                    ImGui.PopID();
                }
            }

            ImGui.Separator();
            if (ImGui.Button("Reset All"))
            {
                foreach (var field in fields)
                {
                    var settingValue = (ISettingValue)field.GetValue(settings)!;
                    settingValue.ResetToDefault();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[SettingsWindow] Error rendering {settings.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Render error: {ex.Message}");
        }
    }

    private static string GetDisplayName(Type settingsType)
    {
        // MovementSettings -> Movement
        // SettingSource -> Login (special case)
        var name = settingsType.Name;

        if (name == "SettingSource")
            return "Login";

        if (name.EndsWith("Settings", StringComparison.Ordinal))
            return name[..^8];

        return name;
    }

    private static string FormatFieldName(string fieldName)
    {
        // DefaultRadius -> Default Radius
        // EnableVisualization -> Enable Visualization
        var sb = new StringBuilder();
        foreach (var c in fieldName)
        {
            if (char.IsUpper(c) && sb.Length > 0)
                sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString();
    }
}
