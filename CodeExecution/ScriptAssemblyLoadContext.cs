#nullable enable

using System.Reflection;
using System.Runtime.Loader;

namespace GoBo.Infrastructure.CodeExecution;

/// <summary>
/// Custom AssemblyLoadContext for script execution that resolves assemblies
/// from the host context to avoid type duplication during hot reload.
/// </summary>
internal sealed class ScriptAssemblyLoadContext : AssemblyLoadContext
{
    private readonly AssemblyLoadContext? _hostContext;

    public ScriptAssemblyLoadContext(AssemblyLoadContext? hostContext)
        : base($"Script_{Guid.NewGuid():N}", isCollectible: true)
    {
        _hostContext = hostContext;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var simple = assemblyName.Name;
        if (string.IsNullOrEmpty(simple))
            return null;

        // Prefer whatever is already in Default (no need to redirect system/DPB assemblies).
        var alreadyDefault = AssemblyLoadContext.Default.Assemblies
            .FirstOrDefault(a => !a.IsDynamic &&
                                 string.Equals(a.GetName().Name, simple, StringComparison.OrdinalIgnoreCase));
        if (alreadyDefault != null)
            return alreadyDefault;

        // If we have a host context (collectible during hot reload), redirect to it
        // to reuse the already-loaded assemblies and avoid type-identity duplication.
        if (_hostContext != null && _hostContext != AssemblyLoadContext.Default)
        {
            var fromHost = _hostContext.Assemblies.FirstOrDefault(a => !a.IsDynamic &&
                                                                        string.Equals(a.GetName().Name, simple,
                                                                            StringComparison.OrdinalIgnoreCase));
            if (fromHost != null)
                return fromHost;
        }

        // Let the default resolution handle it
        return null;
    }
}

