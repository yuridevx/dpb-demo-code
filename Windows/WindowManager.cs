using System.Numerics;
using DreamPoeBot.Loki.Common;
using GoBo.Infrastructure.Lifecycle;
using GoBo.Infrastructure.Modules;
using ImGuiNET;
using log4net;

namespace GoBo.Infrastructure.Windows;

/// <summary>
///     Manages all IWindowModule instances with a unified menu-based navigation.
///     Press F1 to toggle the main window. Windows can be detached to float independently.
/// </summary>
[Module(Priority = Priority.Render)]
public sealed class WindowManager(IScope scope) : IModule, IRenderModule
{
    private const string HotkeyName = "WindowManager.Toggle";
    private static readonly ILog Log = Logger.GetLoggerInstanceForType();

    private readonly HashSet<IWindowModule> _detached = new();
    private IReadOnlyList<IWindowModule> _modules = Array.Empty<IWindowModule>();
    private bool _mainVisible;
    private IWindowModule? _selected;

    public void Initialize()
    {
        Hotkeys.Register(
            HotkeyName,
            Keys.F1,
            ModifierKeys.NoRepeat,
            _ => _mainVisible = !_mainVisible);
    }

    public void Deinitialize()
    {
        Hotkeys.Unregister(HotkeyName);
        _modules = Array.Empty<IWindowModule>();
        _detached.Clear();
        _selected = null;
    }

    public void InitializeRender()
    {
        _modules = scope.ResolveAll<IWindowModule>().ToArray();
        Log.Info($"[WindowManager] Discovered {_modules.Count} window modules");
    }

    public void Render()
    {
        try
        {
            RenderDetachedWindows();
            RenderMainWindow();
        }
        catch (Exception ex)
        {
            Log.Error($"[WindowManager] Render error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void RenderDetachedWindows()
    {
        foreach (var module in _detached.ToList())
        {
            try
            {
                var open = true;
                ImGui.SetNextWindowSize(module.DefaultSize, ImGuiCond.FirstUseEver);

                if (ImGui.Begin(module.Title, ref open))
                {
                    try
                    {
                        module.RenderContent();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[WindowManager] Error rendering detached {module.Title}: {ex.Message}\n{ex.StackTrace}");
                        ImGui.TextColored(new Vector4(0.9f, 0.3f, 0.3f, 1.0f), $"Render error: {ex.Message}");
                    }
                }

                ImGui.End();

                if (!open)
                {
                    _detached.Remove(module);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[WindowManager] Error with detached window {module.Title}: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    private void RenderMainWindow()
    {
        if (!_mainVisible) return;

        // Auto-select first available window if none selected or current is detached
        if (_selected == null || _detached.Contains(_selected))
        {
            _selected = _modules.FirstOrDefault(m => !_detached.Contains(m));
        }

        ImGui.SetNextWindowSize(new Vector2(600, 400), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("GoBo", ref _mainVisible, ImGuiWindowFlags.MenuBar))
        {
            try
            {
                RenderMenuBar();
                RenderContent();
            }
            catch (Exception ex)
            {
                Log.Error($"[WindowManager] Main window error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        ImGui.End();
    }

    private void RenderMenuBar()
    {
        if (!ImGui.BeginMenuBar()) return;

        try
        {
            // Group by category, only show non-detached, skip empty categories
            var grouped = _modules
                .Where(m => !_detached.Contains(m))
                .GroupBy(m => m.Category)
                .Where(g => g.Any())
                .OrderBy(g => g.Min(m => m.Order));

            foreach (var group in grouped)
            {
                var items = group.OrderBy(m => m.Order).ToList();

                if (items.Count == 1)
                {
                    // Single item: direct menu item (no dropdown)
                    var isSelected = _selected == items[0];
                    if (ImGui.MenuItem(items[0].Title, null, isSelected))
                    {
                        _selected = items[0];
                    }
                }
                else
                {
                    // Multiple items: dropdown menu
                    if (ImGui.BeginMenu(group.Key))
                    {
                        foreach (var item in items)
                        {
                            if (ImGui.MenuItem(item.Title, null, _selected == item))
                            {
                                _selected = item;
                            }
                        }

                        ImGui.EndMenu();
                    }
                }
            }

            // Detach button on the right
            if (_selected != null)
            {
                var detachButtonWidth = 24f;
                var availableWidth = ImGui.GetWindowWidth() - ImGui.GetCursorPosX() - detachButtonWidth - 12f;
                if (availableWidth > 0)
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + availableWidth);
                }

                // Style the detach button
                var normalColor = new Vector4(0.3f, 0.5f, 0.7f, 0.7f);
                var hoverColor = new Vector4(0.4f, 0.6f, 0.85f, 1.0f);
                var activeColor = new Vector4(0.2f, 0.4f, 0.6f, 1.0f);
                ImGui.PushStyleColor(ImGuiCol.Button, normalColor);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hoverColor);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeColor);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3f);

                if (ImGui.SmallButton("\u2197"))
                {
                    _detached.Add(_selected);
                    _selected = null;
                }

                ImGui.PopStyleVar();
                ImGui.PopStyleColor(3);

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Detach to floating window");
                }
            }
        }
        finally
        {
            ImGui.EndMenuBar();
        }
    }

    private void RenderContent()
    {
        if (_selected == null)
        {
            ImGui.TextDisabled("Select a window from the menu");
            return;
        }

        try
        {
            _selected.RenderContent();
        }
        catch (Exception ex)
        {
            Log.Error($"[WindowManager] Error rendering {_selected.Title}: {ex.Message}\n{ex.StackTrace}");
            ImGui.TextColored(new Vector4(0.9f, 0.3f, 0.3f, 1.0f), $"Render error: {ex.Message}");
        }
    }
}
