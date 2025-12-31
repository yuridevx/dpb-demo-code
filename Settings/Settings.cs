using System.Reflection;

namespace GoBo.Infrastructure.Settings;

/// <summary>
///     Base class for all settings classes.
///     Provides change notification when any member value changes.
/// </summary>
public abstract class Settings
{
    /// <summary>Raised when any setting value changes.</summary>
    public event Action? Changed;

    /// <summary>
    ///     Called by the factory after all members are initialized.
    ///     Wires up change events from all ISettingValue members.
    /// </summary>
    internal void WireEvents()
    {
        foreach (var field in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            if (field.GetValue(this) is ISettingValue settingValue)
            {
                settingValue.Changed += () => Changed?.Invoke();
            }
        }
    }
}
