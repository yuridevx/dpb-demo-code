namespace GoBo.Infrastructure.Modules;

/// <summary>
///     Lightweight dependency injection container for module resolution.
///     Singletons are cached, prototypes create new instances on each request.
///     Graph is validated upfront during Build().
///     Not thread-safe by design - all resolution must happen on a single thread.
/// </summary>
public sealed partial class Scope : IScope
{
    private readonly List<IFactory> _factories = new();
    private readonly Dictionary<Type, SingletonDef> _singletonDefs = new();
    private readonly Dictionary<Type, PrototypeDef> _prototypeDefs = new();

    private Scope()
    {
    }

    // === Singleton API ===

    public object Resolve(Type type)
    {
        if (_prototypeDefs.ContainsKey(type))
            throw new InvalidOperationException(
                $"Cannot resolve prototype '{type.Name}' via Resolve(). Use New() instead.");

        return EnsureSingletonInstance(FindSingletonType(type));
    }

    public T Resolve<T>()
    {
        return (T)Resolve(typeof(T));
    }

    public IEnumerable<object> ResolveAll(Type type)
    {
        return FindAllSingletonTypes(type).Select(EnsureSingletonInstance);
    }

    public IEnumerable<T> ResolveAll<T>()
    {
        return ResolveAll(typeof(T)).Cast<T>();
    }

    public bool CanResolve(Type type)
    {
        return _singletonDefs.Keys.Any(type.IsAssignableFrom);
    }

    public bool CanResolve<T>()
    {
        return CanResolve(typeof(T));
    }

    // === Prototype API ===

    public object New(Type type)
    {
        if (_singletonDefs.ContainsKey(type))
            throw new InvalidOperationException(
                $"Cannot create singleton '{type.Name}' via New(). Use Resolve() instead.");

        return CreatePrototypeInstance(type);
    }

    public T New<T>()
    {
        return (T)New(typeof(T));
    }

    public bool CanNew(Type type)
    {
        // Singletons cannot be created via New()
        if (_singletonDefs.ContainsKey(type))
            return false;

        // Registered prototypes can always be created
        if (_prototypeDefs.ContainsKey(type))
            return true;

        // Unregistered types - check if constructor can be satisfied
        return CanCreateUnregisteredType(type);
    }

    public bool CanNew<T>()
    {
        return CanNew(typeof(T));
    }

    // === Singleton Type Queries ===

    public IEnumerable<Type> GetRegisteredSingletonTypes()
    {
        return OrderSingletonsByPriority(_singletonDefs.Keys);
    }

    public IEnumerable<Type> GetRegisteredSingletonTypes(Type assignableTo)
    {
        return OrderSingletonsByPriority(_singletonDefs.Keys.Where(assignableTo.IsAssignableFrom));
    }

    public IEnumerable<Type> GetRegisteredSingletonTypes<T>()
    {
        return GetRegisteredSingletonTypes(typeof(T));
    }

    public Type GetRegisteredSingletonType(Type assignableTo)
    {
        var matches = GetRegisteredSingletonTypes(assignableTo).ToList();

        if (matches.Count == 0)
            throw new InvalidOperationException($"No singleton registered for {assignableTo.Name}");
        if (matches.Count > 1)
            throw new InvalidOperationException($"Multiple singletons for {assignableTo.Name}, use GetRegisteredSingletonTypes");

        return matches[0];
    }

    public Type GetRegisteredSingletonType<T>()
    {
        return GetRegisteredSingletonType(typeof(T));
    }

    // === Prototype Type Queries ===

    public IEnumerable<Type> GetRegisteredPrototypeTypes()
    {
        return OrderPrototypesByPriority(_prototypeDefs.Keys);
    }

    public IEnumerable<Type> GetRegisteredPrototypeTypes(Type assignableTo)
    {
        return OrderPrototypesByPriority(_prototypeDefs.Keys.Where(assignableTo.IsAssignableFrom));
    }

    public IEnumerable<Type> GetRegisteredPrototypeTypes<T>()
    {
        return GetRegisteredPrototypeTypes(typeof(T));
    }

    public Type GetRegisteredPrototypeType(Type assignableTo)
    {
        var matches = GetRegisteredPrototypeTypes(assignableTo).ToList();

        if (matches.Count == 0)
            throw new InvalidOperationException($"No prototype registered for {assignableTo.Name}");
        if (matches.Count > 1)
            throw new InvalidOperationException($"Multiple prototypes for {assignableTo.Name}, use GetRegisteredPrototypeTypes");

        return matches[0];
    }

    public Type GetRegisteredPrototypeType<T>()
    {
        return GetRegisteredPrototypeType(typeof(T));
    }

    // === Factory ===

    public static Scope Create(IReadOnlyList<Type> types, params object[] instances)
    {
        var scope = new Scope();

        scope._singletonDefs[typeof(Scope)] = new SingletonDef(typeof(Scope)) { Instance = scope };

        foreach (var instance in instances)
            scope._singletonDefs[instance.GetType()] = new SingletonDef(instance.GetType()) { Instance = instance };

        scope.Build(types);

        return scope;
    }
}
