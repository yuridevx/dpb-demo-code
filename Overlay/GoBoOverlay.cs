using System.Runtime.InteropServices;
using DreamPoeBot.Loki.Common;
using GoBo.Infrastructure.Lifecycle;
using ImGuiNET;
using log4net;
using Microsoft.Win32;

namespace GoBo.Infrastructure.Overlay;

public sealed class GoBoOverlay(IReadOnlyList<IRenderModule> renderModules) : ClickableTransparentOverlay.Overlay
{
    private const int GwlExstyle = -20;
    private const int WsExToolwindow = 0x00000080;
    private const int WsExAppwindow = 0x00040000;
    private const int SmCxscreen = 0;
    private const int SmCyscreen = 1;
    private const uint SwpNoZorder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowwindow = 0x0040;

    private static readonly ILog Log = Logger.GetLoggerInstanceForType();

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags);

    protected override Task PostInitialized()
    {
        // Enable VSync and FPS limit to ensure proper frame timing and prevent DeltaTime = 0 assertion
        // This fixes the "Need a positive DeltaTime!" error on Start/Stop cycles
        VSync = true;
        FPSLimit = 60;

        var overlayHandle = window.Handle;

        var exStyle = GetWindowLong(overlayHandle, GwlExstyle);
        exStyle |= WsExToolwindow;
        exStyle &= ~WsExAppwindow;
        SetWindowLong(overlayHandle, GwlExstyle, exStyle);

        Log.Info($"[GoBoOverlay] Window created (0x{overlayHandle:X}) - hidden from taskbar.");

        // Initialize render modules
        foreach (var module in renderModules)
            try
            {
                module.InitializeRender();
            }
            catch (Exception e)
            {
                Log.Error(
                    $"[GoBoOverlay] Exception while initializing render module: {module.GetType().Name}: {e.Message}",
                    e);
            }

        ResizeToFullscreen();
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        return Task.CompletedTask;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        ResizeToFullscreen();
    }

    private void ResizeToFullscreen()
    {
        var overlayHandle = window.Handle;
        if (overlayHandle == IntPtr.Zero) return;

        var width = GetSystemMetrics(SmCxscreen);
        var height = GetSystemMetrics(SmCyscreen);
        if (width <= 0 || height <= 0)
        {
            Log.Warn("[GoBoOverlay] Invalid screen dimensions detected, skipping resize.");
            return;
        }

        if (!SetWindowPos(
                overlayHandle,
                IntPtr.Zero,
                0,
                0,
                width,
                height,
                SwpNoZorder | SwpNoActivate | SwpShowwindow))
            Log.Warn("[GoBoOverlay] Failed to resize overlay to fullscreen.");
        else
            Log.Info($"[GoBoOverlay] Resized to fullscreen: {width}x{height}.");
    }

    protected override void Render()
    {
        foreach (var module in renderModules)
            try
            {
                module.Render();
            }
            catch (Exception e)
            {
                Log.Error("[GoBoOverlay] Exception while rendering module: " + e.Message, e);
            }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            Log.Info("[GoBoOverlay] Disposing...");
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        }

        // Destroy ImGui context to prevent stale state (FrameCount > 0) on restart
        // The library doesn't do this, causing "Need a positive DeltaTime!" assertion
        try
        {
            var ctx = ImGui.GetCurrentContext();
            if (ctx != IntPtr.Zero)
            {
                ImGui.DestroyContext(ctx);
                Log.Info("[GoBoOverlay] ImGui context destroyed.");
            }
        }
        catch (Exception e)
        {
            Log.Warn($"[GoBoOverlay] Failed to destroy ImGui context: {e.Message}");
        }
    }
}