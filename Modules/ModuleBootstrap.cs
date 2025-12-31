using System.Reflection;

namespace GoBo.Infrastructure.Modules;

/// <summary>
///     Discovers [Module] and [Prototype] annotated types from assemblies.
/// </summary>
internal static class ModuleBootstrap
{
    public static IScope CreateDefault()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var types = DiscoverTypes(assembly);
        return Scope.Create(types);
    }

    private static IReadOnlyList<Type> DiscoverTypes(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .Where(t => !t.IsSubclassOf(typeof(Attribute)))
            .Where(t => HasModuleAttribute(t) || HasPrototypeAttribute(t))
            .Where(IsEnabled)
            .ToArray();
    }

    private static bool HasModuleAttribute(Type type)
    {
        return type.GetCustomAttribute<ModuleAttribute>() != null;
    }

    private static bool HasPrototypeAttribute(Type type)
    {
        return type.GetCustomAttribute<PrototypeAttribute>() != null;
    }

    private static bool IsEnabled(Type type)
    {
        var moduleAttr = type.GetCustomAttribute<ModuleAttribute>();
        if (moduleAttr is { Enabled: false })
            return false;

        var prototypeAttr = type.GetCustomAttribute<PrototypeAttribute>();
        if (prototypeAttr is { Enabled: false })
            return false;

        return true;
    }
}