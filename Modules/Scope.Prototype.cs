using System.Reflection;

namespace GoBo.Infrastructure.Modules;

/// <summary>
///     Prototype resolution methods.
/// </summary>
public sealed partial class Scope
{
    private IEnumerable<Type> OrderPrototypesByPriority(IEnumerable<Type> types)
    {
        return types
            .OrderBy(t => _prototypeDefs.TryGetValue(t, out var def) ? def.Priority : 0)
            .ThenBy(t => t.FullName, StringComparer.OrdinalIgnoreCase);
    }

    private bool CanCreateUnregisteredType(Type type)
    {
        var constructors = type
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderByDescending(c => c.GetParameters().Length)
            .ToList();

        // Parameterless constructor always works
        if (constructors.Count == 0 || constructors.Any(c => c.GetParameters().Length == 0))
            return true;

        // Check if any constructor can be satisfied
        return constructors.Any(CanSatisfyUnregisteredConstructor);
    }

    private object CreatePrototypeInstance(Type type)
    {
        // Registered prototype - use pre-selected constructor
        if (_prototypeDefs.TryGetValue(type, out var def))
            return CreatePrototypeInstance(def);

        // Unregistered type - create on-the-fly with DI
        return CreateUnregisteredInstance(type);
    }

    private object CreateUnregisteredInstance(Type type)
    {
        var constructors = type
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderByDescending(c => c.GetParameters().Length)
            .ToList();

        if (constructors.Count == 0)
            return Activator.CreateInstance(type)
                ?? throw new InvalidOperationException($"Could not create instance of {type.Name}");

        foreach (var ctor in constructors)
        {
            if (CanSatisfyUnregisteredConstructor(ctor))
            {
                var args = ctor.GetParameters()
                    .Select(p => ResolveUnregisteredParameter(p.ParameterType))
                    .ToArray();
                return ctor.Invoke(args);
            }
        }

        throw new InvalidOperationException($"No satisfiable constructor found for {type.Name}");
    }

    private bool CanSatisfyUnregisteredConstructor(ConstructorInfo ctor)
    {
        foreach (var param in ctor.GetParameters())
        {
            if (IsCollectionType(param.ParameterType))
                continue;

            if (param.ParameterType == typeof(IScope))
                continue;

            if (!CanSatisfyUnregisteredSingletonParameter(param.ParameterType))
                return false;
        }

        return true;
    }

    private bool CanSatisfyUnregisteredSingletonParameter(Type paramType)
    {
        return _singletonDefs.Keys.Any(paramType.IsAssignableFrom);
    }

    private object ResolveUnregisteredParameter(Type paramType)
    {
        if (paramType == typeof(IScope))
            return this;

        if (paramType.IsArray)
            return CreateSingletonArray(paramType.GetElementType()!);

        if (IsCollectionType(paramType))
            return CreateSingletonList(paramType.GetGenericArguments()[0]);

        return Resolve(paramType);
    }
}
