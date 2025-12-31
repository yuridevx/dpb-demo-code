namespace GoBo.Infrastructure.Logic;

/// <summary>
///     Context for IBranch - only non-blocking operations.
/// </summary>
public interface IBranchContext
{
    /// <summary>
    ///     Declare a component as active for this tick.
    /// </summary>
    void Use<T>() where T : IComponent;
}