namespace GoBo.Infrastructure.Logic;

/// <summary>
///     Sequential execution, runs to completion (async).
/// </summary>
public interface IFlow : IComponent
{
    Task Run(IFlowContext ctx);

    void OnCancel()
    {
    }
}