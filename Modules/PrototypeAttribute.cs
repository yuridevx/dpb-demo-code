namespace GoBo.Infrastructure.Modules;

/// <summary>
///     Marks a class as a prototype for DI registration.
///     Prototype types create a new instance on each New() call.
///     Use New&lt;T&gt;() to create instances - Resolve&lt;T&gt;() will throw.
/// </summary>
/// <remarks>
///     Prototypes cannot be:
///     - Resolved via Resolve&lt;T&gt;() - use New&lt;T&gt;() instead
///     - Injected as constructor dependencies of singletons
///     - Included in collection parameters
///
///     Prototypes can depend on singletons and other prototypes.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PrototypeAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets whether the prototype is enabled.
    ///     Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Gets or sets the prototype's priority order.
    ///     Lower values have higher priority.
    ///     Default is 0.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    ///     Gets or sets the prototype's display name.
    ///     Optional - if not set, the class name will be used.
    /// </summary>
    public string? DisplayName { get; set; }
}
