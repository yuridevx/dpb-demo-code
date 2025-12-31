using DreamPoeBot.Loki.Common;
using GoBo.Infrastructure.Lifecycle;
using GoBo.Infrastructure.Modules;
using log4net;

namespace GoBo.Infrastructure.Overlay;

[Module(Priority = Priority.Overlay)]
internal sealed class OverlayHost(IReadOnlyList<IRenderModule> renderModules) : IModule, IStartStopModule
{
    private static readonly ILog Log = Logger.GetLoggerInstanceForType();

    private GoBoOverlay? _overlay;
    private Task? _overlayTask;

    public void Initialize()
    {
    }

    public void Deinitialize()
    {
        _overlay?.Dispose();
        _overlay = null;
    }

    public void Start()
    {
        if (_overlayTask != null) return;
        _overlayTask = Task.Run(RunOverlayLoop);
    }

    public void Stop()
    {
        _overlay?.Close();
        WaitForOverlayTask();
        _overlay?.Dispose();
        _overlay = null;
        _overlayTask = null;
        GC.Collect();
    }

    private async Task RunOverlayLoop()
    {
        try
        {
            Log.Info("[OverlayHost] Initializing Overlay...");
            _overlay = new GoBoOverlay(renderModules);
            await _overlay.Run();
        }
        catch (Exception ex)
        {
            Log.Error($"[OverlayHost] Overlay crashed: {ex.Message}", ex);
        }
    }

    private void WaitForOverlayTask()
    {
        if (_overlayTask == null) return;

        Log.Info("[OverlayHost] Waiting for overlay task to finish...");
        try
        {
            if (!_overlayTask.Wait(TimeSpan.FromSeconds(5)))
                Log.Warn("[OverlayHost] Overlay task did not complete within timeout.");
            else
                Log.Info("[OverlayHost] Overlay task completed successfully.");
        }
        catch (AggregateException ex)
        {
            Log.Error("[OverlayHost] Overlay task faulted while shutting down.", ex);
        }
    }
}