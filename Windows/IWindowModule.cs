using System.Numerics;

namespace GoBo.Infrastructure.Windows;

/// <summary>
///     Interface for windows that can be displayed in the WindowManager.
///     Implement this interface to add a new window to the navigation system.
/// </summary>
public interface IWindowModule
{
    /// <summary>
    ///     The window title displayed in the menu and window title bar.
    /// </summary>
    string Title { get; }

    /// <summary>
    ///     The category for grouping in the menu bar.
    ///     Windows with the same category are grouped together.
    /// </summary>
    string Category { get; }

    /// <summary>
    ///     Sort order within the category. Lower values appear first.
    /// </summary>
    int Order => 0;

    /// <summary>
    ///     Default size when the window is detached.
    /// </summary>
    Vector2 DefaultSize => new(400, 300);

    /// <summary>
    ///     Renders the window content (without Begin/End).
    ///     The WindowManager handles the window chrome.
    /// </summary>
    void RenderContent();
}
