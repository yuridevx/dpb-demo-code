using System.Reflection;

namespace GoBo.Infrastructure.Modules;

/// <summary>
///     Build phase: registration and validation.
/// </summary>
public sealed partial class Scope
{
    private void Build(IReadOnlyList<Type> types)
    {
        RegisterTypes(types);
        SelectAllConstructors();
        ValidatePrototypeUsage();
        VerifyNoCycles();
        InstantiateSingletonFactories();
        AssignFactoriesToTypes();
    }

    private void RegisterTypes(IReadOnlyList<Type> types)
    {
        foreach (var type in types)
        {
            var moduleAttr = type.GetCustomAttribute<ModuleAttribute>();
            var prototypeAttr = type.GetCustomAttribute<PrototypeAttribute>();

            var priority = prototypeAttr?.Priority ?? moduleAttr?.Priority ?? 0;

            if (prototypeAttr != null)
                _prototypeDefs[type] = new PrototypeDef(type) { Priority = priority };
            else
                _singletonDefs[type] = new SingletonDef(type) { Priority = priority };
        }
    }

    private void SelectAllConstructors()
    {
        foreach (var def in _singletonDefs.Values)
            if (def.Instance == null)
                def.Constructor = SelectConstructor(def.Type);

        foreach (var def in _prototypeDefs.Values)
            def.Constructor = SelectConstructor(def.Type);
    }

    private ConstructorInfo SelectConstructor(Type type)
    {
        var constructors = type
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderByDescending(c => c.GetParameters().Length)
            .ToList();

        foreach (var ctor in constructors)
            if (CanSatisfyConstructor(ctor))
                return ctor;

        var details = new System.Text.StringBuilder();
        details.AppendLine($"No satisfiable constructor for {type.FullName}");
        details.AppendLine($"Found {constructors.Count} constructor(s):");

        foreach (var ctor in constructors)
        {
            var parameters = ctor.GetParameters();
            if (parameters.Length == 0)
            {
                details.AppendLine("  .ctor() - parameterless");
            }
            else
            {
                var paramList = string.Join(", ", parameters.Select(p => $"{p.ParameterType.FullName} {p.Name}"));
                details.AppendLine($"  .ctor({paramList})");

                foreach (var param in parameters)
                {
                    var paramType = param.ParameterType;
                    var canSatisfy = CanSatisfyParameter(paramType);

                    if (canSatisfy)
                    {
                        details.AppendLine($"    [OK] {paramType.FullName} {param.Name}");
                    }
                    else if (IsPrototypeDependency(paramType))
                    {
                        details.AppendLine($"    [PROTOTYPE] {paramType.FullName} {param.Name} - cannot depend on prototype, use IScope.New<T>()");
                    }
                    else
                    {
                        details.AppendLine($"    [MISSING] {paramType.FullName} {param.Name}");
                    }
                }
            }
        }

        throw new InvalidOperationException(details.ToString());
    }

    private bool CanSatisfyConstructor(ConstructorInfo ctor)
    {
        foreach (var param in ctor.GetParameters())
            if (!CanSatisfyParameter(param.ParameterType))
                return false;

        return true;
    }

    private bool CanSatisfyParameter(Type paramType)
    {
        if (IsCollectionType(paramType))
            return true; // Empty collections are valid

        return _singletonDefs.Keys.Any(paramType.IsAssignableFrom);
    }

    private bool IsPrototypeDependency(Type paramType)
    {
        return _prototypeDefs.Keys.Any(paramType.IsAssignableFrom);
    }

    // === Singleton Factory Methods ===

    private void InstantiateSingletonFactories()
    {
        var factoryTypes = _singletonDefs.Keys
            .Where(t => typeof(IFactory).IsAssignableFrom(t))
            .ToList();

        foreach (var factoryType in factoryTypes)
        {
            var def = _singletonDefs[factoryType];
            var factory = (IFactory)CreateViaConstructor(def.Type, def.Constructor);
            def.Instance = factory;
            _factories.Add(factory);
        }
    }

    private void AssignFactoriesToTypes()
    {
        foreach (var def in _singletonDefs.Values)
            if (def.Instance == null)
                def.Factory = FindFactoryForType(def.Type);

        foreach (var def in _prototypeDefs.Values)
            def.Factory = FindFactoryForType(def.Type);
    }

    private IFactory FindFactoryForType(Type type)
    {
        var candidates = _factories
            .Select(f => (factory: f, distance: CalculateDistance(type, f.TargetType)))
            .Where(x => x.distance >= 0)
            .ToList();

        if (candidates.Count == 0) return null;

        var minDistance = candidates.Min(c => c.distance);
        var best = candidates.Where(c => c.distance == minDistance).ToList();

        if (best.Count > 1)
            throw new InvalidOperationException(
                $"Multiple factories with same distance for {type.Name}: " +
                string.Join(", ", best.Select(b => b.factory.GetType().Name)));

        return best[0].factory;
    }

    // === Prototype Validation ===

    private void ValidatePrototypeUsage()
    {
        foreach (var def in _singletonDefs.Values)
            ValidateNoPrototypesInCollectionParameters(def.Type, def.Constructor);

        foreach (var def in _prototypeDefs.Values)
            ValidateNoPrototypesInCollectionParameters(def.Type, def.Constructor);
    }

    private void ValidateNoPrototypesInCollectionParameters(Type ownerType, ConstructorInfo constructor)
    {
        if (constructor == null) return;

        foreach (var param in constructor.GetParameters())
        {
            var elementType = GetElementTypeIfCollection(param.ParameterType);
            if (elementType == null) continue;

            var prototypeInCollection = _prototypeDefs.Values
                .FirstOrDefault(def => elementType.IsAssignableFrom(def.Type));

            if (prototypeInCollection != null)
            {
                throw new InvalidOperationException(
                    $"Prototype '{prototypeInCollection.Type.Name}' cannot be resolved via collection " +
                    $"'{param.ParameterType.Name}' in '{ownerType.Name}'. " +
                    "Inject IScope and use New<T>() to create prototypes on demand.");
            }
        }
    }

    private static Type? GetElementTypeIfCollection(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType && CollectionTypes.Contains(type.GetGenericTypeDefinition()))
            return type.GetGenericArguments()[0];

        return null;
    }

    // === Cycle Detection ===

    private void VerifyNoCycles()
    {
        var graph = BuildDependencyGraph();

        var visiting = new HashSet<Type>();
        var visited = new HashSet<Type>();

        foreach (var type in graph.Keys)
            if (DetectCycle(type, graph, visiting, visited, out var cycle))
                throw new InvalidOperationException(
                    $"Circular dependency: {string.Join(" → ", cycle.Select(t => t.Name))}");
    }

    private Dictionary<Type, List<Type>> BuildDependencyGraph()
    {
        var graph = new Dictionary<Type, List<Type>>();

        BuildSingletonDependencyGraph(graph);
        BuildPrototypeDependencyGraph(graph);

        return graph;
    }

    private void BuildSingletonDependencyGraph(Dictionary<Type, List<Type>> graph)
    {
        foreach (var def in _singletonDefs.Values)
            graph[def.Type] = GetSingletonDependencies(def.Constructor);
    }

    private void BuildPrototypeDependencyGraph(Dictionary<Type, List<Type>> graph)
    {
        foreach (var def in _prototypeDefs.Values)
            graph[def.Type] = GetSingletonDependencies(def.Constructor);
    }

    private List<Type> GetSingletonDependencies(ConstructorInfo constructor)
    {
        var deps = new List<Type>();
        if (constructor == null) return deps;

        foreach (var param in constructor.GetParameters())
        {
            var elementType = GetElementType(param.ParameterType);

            foreach (var singletonType in _singletonDefs.Keys)
                if (elementType.IsAssignableFrom(singletonType))
                    deps.Add(singletonType);
        }

        return deps;
    }

    private static bool DetectCycle(
        Type type,
        Dictionary<Type, List<Type>> graph,
        HashSet<Type> visiting,
        HashSet<Type> visited,
        out List<Type> cycle)
    {
        cycle = null;

        if (visited.Contains(type)) return false;
        if (visiting.Contains(type))
        {
            cycle = [type];
            return true;
        }

        visiting.Add(type);

        if (graph.TryGetValue(type, out var deps))
            foreach (var dep in deps)
                if (DetectCycle(dep, graph, visiting, visited, out cycle))
                {
                    cycle.Insert(0, type);
                    return true;
                }

        visiting.Remove(type);
        visited.Add(type);
        return false;
    }
}
