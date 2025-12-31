namespace GoBo.Infrastructure.Modules;

/// <summary>
///     Singleton resolution methods.
/// </summary>
public sealed partial class Scope
{
    private Type FindSingletonType(Type type)
    {
        var matches = FindAllSingletonTypes(type).ToList();

        if (matches.Count == 0)
            throw new InvalidOperationException($"No singleton registered for {type.Name}");
        if (matches.Count > 1)
            throw new InvalidOperationException($"Multiple singletons for {type.Name}, use ResolveAll");

        return matches[0];
    }

    private IEnumerable<Type> FindAllSingletonTypes(Type type)
    {
        return OrderSingletonsByPriority(_singletonDefs.Keys.Where(type.IsAssignableFrom));
    }

    private IEnumerable<Type> OrderSingletonsByPriority(IEnumerable<Type> types)
    {
        return types
            .OrderBy(t => _singletonDefs.TryGetValue(t, out var def) ? def.Priority : 0)
            .ThenBy(t => t.FullName, StringComparer.OrdinalIgnoreCase);
    }

    private object EnsureSingletonInstance(Type type)
    {
        if (!_singletonDefs.TryGetValue(type, out var def))
            throw new InvalidOperationException($"Type {type.Name} not registered as singleton");

        if (def.Instance != null)
            return def.Instance;

        var instance = CreateSingletonInstance(def);
        def.Instance = instance;
        return instance;
    }
}
