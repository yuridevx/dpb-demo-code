namespace GoBo.Infrastructure.Logic;

/// <summary>
///     Declarative selection, evaluated each tick (sync).
/// </summary>
public interface IBranch : IComponent
{
    void Tick(IBranchContext ctx);
}