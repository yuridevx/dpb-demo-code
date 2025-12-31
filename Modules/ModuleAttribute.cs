namespace GoBo.Infrastructure.Modules;

/// <summary>
///     Required attribute for DI auto-discovery.
///     Only classes annotated with [Module] are registered in the DI container
///     during bootstrap. Use this to control module behavior and priority.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ModuleAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets whether the module is enabled.
    ///     Default is true if attribute is not present.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Gets or sets the module's priority order.
    ///     Lower values have higher priority.
    ///     Default is 0 if attribute is not present.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    ///     Gets or sets the module's display name.
    ///     Optional - if not set, the class name will be used.
    /// </summary>
    public string? DisplayName { get; set; }
}