using DreamPoeBot.Common;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Controllers;
using DreamPoeBot.Loki.Game;
using GoBo.Infrastructure.Drawing.Drawables;
using GoBo.Infrastructure.Extensions;
using GoBo.Infrastructure.Lifecycle;
using GoBo.Infrastructure.Modules;
using log4net;
using Vector2 = System.Numerics.Vector2;
using Vector2i = DreamPoeBot.Common.Vector2i;
using Vector3 = System.Numerics.Vector3;

namespace GoBo.Infrastructure.Drawing;

/// <summary>
///     Drawing canvas that manages drawable objects using a transactional approach.
///     After all ticks complete, drawables are committed and replace the previous render set.
///     Owned and managed by Plugin, used by CanvasRenderer for rendering.
/// </summary>
[Module(Priority = Priority.Render)]
public sealed class DrawingCanvas : IModule, IRenderModule
{
    private static readonly ILog Log = Logger.GetLoggerInstanceForType();

    // Double-buffering: one for building during ticks, one for rendering
    private List<Drawable> _buildBuffer;
    private ReaderWriterLockSlim _lock;
    private List<Drawable> _renderBuffer;

    public void Initialize()
    {
        _lock = new ReaderWriterLockSlim();
        _buildBuffer = new List<Drawable>();
        _renderBuffer = new List<Drawable>();
    }

    public void Deinitialize()
    {
        _lock?.Dispose();
        _lock = null;
        _buildBuffer = null;
        _renderBuffer = null;
    }

    public void Render()
    {
        var window = GameController.Instance?.Window;
        if (window == null)
            return;

        var offset = window.GetWindowRectangleReal();
        var offsetVector = new Vector2(offset.X, offset.Y);

        _lock.EnterReadLock();
        try
        {
            // Separate text drawables for overlap resolution
            var textDrawables = new List<TextDrawable>();
            var otherDrawables = new List<Drawable>();

            foreach (var drawable in _renderBuffer)
                if (drawable is TextDrawable text)
                    textDrawables.Add(text);
                else
                    otherDrawables.Add(drawable);

            // Render non-text drawables first
            foreach (var drawable in otherDrawables)
            {
                try
                {
                    drawable.Draw(offsetVector);
                }
                catch (Exception ex)
                {
                    Log.Error($"[DrawingCanvas] Drawable render error ({drawable.GetType().Name}): {ex.Message}");
                }
            }

            // Resolve text overlaps and render
            ResolveTextOverlaps(textDrawables);
            foreach (var text in textDrawables)
            {
                try
                {
                    text.Draw(offsetVector);
                }
                catch (Exception ex)
                {
                    Log.Error($"[DrawingCanvas] Text render error: {ex.Message}");
                }
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    ///     Commits the current build buffer to the render buffer.
    ///     Called by CanvasRenderer after all Tick modules have run.
    /// </summary>
    internal void Commit()
    {
        _lock.EnterWriteLock();
        try
        {
            // Swap buffers - build buffer becomes render buffer
            (_buildBuffer, _renderBuffer) = (_renderBuffer, _buildBuffer);

            // Clear the new build buffer for the next frame
            _buildBuffer.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private static Vector3 MapToWorldPosition(Vector2i mapPosition)
    {
        return mapPosition.MapToWorld3().ToNumerics();
    }

    private void AddDrawable(Drawable drawable)
    {
        _lock.EnterWriteLock();
        try
        {
            _buildBuffer.Add(drawable);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private static void ResolveTextOverlaps(List<TextDrawable> textDrawables)
    {
        if (textDrawables.Count < 2)
            return;

        // Sort by Y position, then X position for consistent ordering
        textDrawables.Sort((a, b) =>
        {
            var yCompare = a.Position.Y.CompareTo(b.Position.Y);
            return yCompare != 0 ? yCompare : a.Position.X.CompareTo(b.Position.X);
        });

        // Track occupied regions as (minY, maxY) for each text
        var occupiedRegions = new List<(float minY, float maxY, float minX, float maxX)>();

        foreach (var text in textDrawables)
        {
            var size = text.GetTextSize();
            if (size == Vector2.Zero)
                continue;

            var pos = text.Position;
            var minX = pos.X;
            var maxX = pos.X + size.X;
            var minY = pos.Y;
            var maxY = pos.Y + size.Y;

            // Check for overlaps and adjust Y position
            bool hasOverlap;
            do
            {
                hasOverlap = false;
                foreach (var region in occupiedRegions)
                    // Check if rectangles overlap
                    if (minX < region.maxX && maxX > region.minX &&
                        minY < region.maxY && maxY > region.minY)
                    {
                        // Move text below the overlapping region
                        var offset = region.maxY - minY + 2; // 2px padding
                        minY += offset;
                        maxY += offset;
                        hasOverlap = true;
                        break;
                    }
            } while (hasOverlap);

            // Update text position if it changed
            if (minY != pos.Y)
                text.Position = new Vector2(pos.X, minY);

            // Add to occupied regions
            occupiedRegions.Add((minY, maxY, minX, maxX));
        }
    }


    #region Screen Coordinate Methods

    public void ScreenCircle(
        Vector2 center,
        float radius,
        uint color,
        bool filled = false,
        float thickness = 1f)
    {
        if (radius <= 0)
            return;

        var circle = new CircleDrawable
        {
            Center = center,
            Radius = radius,
            Color = color,
            Filled = filled,
            Thickness = thickness
        };

        AddDrawable(circle);
    }

    public void ScreenLine(
        Vector2 start,
        Vector2 end,
        uint color,
        float thickness = 1f)
    {
        var line = new LineDrawable
        {
            Start = start,
            End = end,
            Color = color,
            Thickness = thickness
        };

        AddDrawable(line);
    }

    public void ScreenBox(
        Vector2 topLeft,
        Vector2 bottomRight,
        uint color,
        bool filled = false,
        float thickness = 1f)
    {
        var box = new BoxDrawable
        {
            TopLeft = topLeft,
            BottomRight = bottomRight,
            Color = color,
            Filled = filled,
            Thickness = thickness
        };

        AddDrawable(box);
    }

    public void ScreenText(
        Vector2 position,
        string text,
        uint color = 0xFFFFFFFF)
    {
        if (string.IsNullOrWhiteSpace(text))
            text = string.Empty;

        var label = new TextDrawable
        {
            Position = position,
            Text = text,
            Color = color
        };

        AddDrawable(label);
    }

    public void ScreenArrow(
        Vector2 start,
        Vector2 end,
        uint color,
        float thickness = 1f,
        float headLength = 10f)
    {
        var arrow = new ArrowDrawable
        {
            Start = start,
            End = end,
            Color = color,
            Thickness = thickness,
            HeadLength = headLength
        };

        AddDrawable(arrow);
    }

    #endregion

    #region World Coordinate Methods

    public void WorldCircle(
        Vector3 center,
        float radius,
        uint color,
        bool filled = false,
        float thickness = 1f)
    {
        if (!LokiPoe.IsInGame || radius <= 0)
            return;

        var centerScreen = center.WorldToScreen();

        // Convert world radius to screen radius
        var edgeWorld = new Vector3(center.X + radius, center.Y, center.Z);
        var edgeScreen = edgeWorld.WorldToScreen();
        var screenRadius = Math.Abs(edgeScreen.X - centerScreen.X);

        ScreenCircle(centerScreen, screenRadius, color, filled, thickness);
    }

    public void WorldLine(
        Vector3 start,
        Vector3 end,
        uint color,
        float thickness = 1f)
    {
        if (!LokiPoe.IsInGame)
            return;

        var startScreen = start.WorldToScreen();
        var endScreen = end.WorldToScreen();

        ScreenLine(startScreen, endScreen, color, thickness);
    }

    public void WorldBox(
        Vector3 center,
        Vector2 size,
        uint color,
        bool filled = false,
        float thickness = 1f)
    {
        if (!LokiPoe.IsInGame)
            return;

        // Convert box corners to screen space
        var topLeft = new Vector3(center.X - size.X / 2, center.Y - size.Y / 2, center.Z)
            .WorldToScreen();
        var bottomRight = new Vector3(center.X + size.X / 2, center.Y + size.Y / 2, center.Z)
            .WorldToScreen();

        ScreenBox(topLeft, bottomRight, color, filled, thickness);
    }

    public void WorldText(
        Vector3 worldPosition,
        string text,
        uint color = 0xFFFFFFFF)
    {
        if (!LokiPoe.IsInGame)
            return;

        var screenPos = worldPosition.WorldToScreen();
        ScreenText(screenPos, text, color);
    }

    public void WorldArrow(
        Vector3 start,
        Vector3 end,
        uint color,
        float thickness = 1f,
        float headLength = 10f)
    {
        if (!LokiPoe.IsInGame)
            return;

        var startScreen = start.WorldToScreen();
        var endScreen = end.WorldToScreen();

        ScreenArrow(startScreen, endScreen, color, thickness, headLength);
    }

    #endregion

    #region Map Coordinate Methods

    public void MapCircle(
        Vector2i center,
        float radius,
        uint color,
        bool filled = false,
        float thickness = 1f)
    {
        if (!LokiPoe.IsInGame)
            return;

        var worldPos = MapToWorldPosition(center);
        WorldCircle(worldPos, radius, color, filled, thickness);
    }

    public void MapLine(
        Vector2i start,
        Vector2i end,
        uint color,
        float thickness = 1f)
    {
        if (!LokiPoe.IsInGame)
            return;

        var worldStart = MapToWorldPosition(start);
        var worldEnd = MapToWorldPosition(end);
        WorldLine(worldStart, worldEnd, color, thickness);
    }

    public void MapBox(
        Vector2i center,
        Vector2 size,
        uint color,
        bool filled = false,
        float thickness = 1f)
    {
        if (!LokiPoe.IsInGame)
            return;

        var worldPos = MapToWorldPosition(center);
        WorldBox(worldPos, size, color, filled, thickness);
    }

    public void MapText(
        Vector2i mapPosition,
        string text,
        uint color = 0xFFFFFFFF)
    {
        if (!LokiPoe.IsInGame)
            return;

        var worldPos = MapToWorldPosition(mapPosition);
        WorldText(worldPos, text, color);
    }

    public void MapArrow(
        Vector2i start,
        Vector2i end,
        uint color,
        float thickness = 1f,
        float headLength = 10f)
    {
        if (!LokiPoe.IsInGame)
            return;

        var worldStart = MapToWorldPosition(start);
        var worldEnd = MapToWorldPosition(end);
        WorldArrow(worldStart, worldEnd, color, thickness, headLength);
    }

    public void MapDirectionArrow(
        Vector2i position,
        Vector2i direction,
        float length,
        uint color,
        float thickness = 2f,
        float headLength = 12f)
    {
        if (!LokiPoe.IsInGame || direction == Vector2i.Zero)
            return;

        // Calculate end point based on direction and length
        var endX = position.X + (int)(direction.X * length);
        var endY = position.Y + (int)(direction.Y * length);
        var end = new Vector2i(endX, endY);

        MapArrow(position, end, color, thickness, headLength);
    }

    #endregion
}