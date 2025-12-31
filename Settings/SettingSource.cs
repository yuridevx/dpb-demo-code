using GoBo.Infrastructure.Modules;
using GoBo.Infrastructure.Settings.Values;

namespace GoBo.Infrastructure.Settings;

/// <summary>
///     Global settings for login credentials and character selection.
/// </summary>
[Module]
public sealed class SettingSource : Settings
{
    public StringValue Email = new();
    public StringValue Password = new();
    public StringValue Gateway = new();
    public StringValue Character = new();
}
