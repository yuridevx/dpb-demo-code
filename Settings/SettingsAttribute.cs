namespace GoBo.Infrastructure.Settings;

/// <summary>
///     Optional attribute to override the default filename for a settings type.
///     If not specified, filename is inferred from class name:
///     "AutocraftSettings" → "autocraft.json"
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SettingsAttribute(string fileName) : Attribute
{
    public string FileName { get; } = fileName;
}