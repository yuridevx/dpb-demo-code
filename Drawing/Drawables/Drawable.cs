using System.Numerics;

namespace GoBo.Infrastructure.Drawing.Drawables;

/// <summary>
///     Base class for all drawable objects in the rendering system.
/// </summary>
public abstract class Drawable
{
    /// <summary>
    ///     Draw this object to the screen with the specified offset.
    /// </summary>
    /// <param name="screenOffset">Offset to apply to screen coordinates.</param>
    public abstract void Draw(Vector2 screenOffset);
}