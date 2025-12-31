using GoBo.Infrastructure.Modules;

namespace GoBo.Infrastructure.CodeExecution;

public class ScriptGlobals(IScope scope)
{
    public IScope Scope { get; } = scope;
}


