using System.Collections;
using System.Reflection;

namespace GoBo.Infrastructure.Modules;

/// <summary>
///     Instance creation logic for singletons and prototypes.
/// </summary>
public sealed partial class Scope
{
    private object CreateSingletonInstance(SingletonDef def)
    {
        return def.Factory != null
            ? def.Factory.Create(this, def.Type)
            : CreateViaConstructor(def.Type, def.Constructor);
    }

    private object CreatePrototypeInstance(PrototypeDef def)
    {
        return def.Factory != null
            ? def.Factory.Create(this, def.Type)
            : CreateViaConstructor(def.Type, def.Constructor);
    }

    private object CreateViaConstructor(Type type, ConstructorInfo constructor)
    {
        var args = constructor.GetParameters()
            .Select(p => ResolveConstructorParameter(p.ParameterType))
            .ToArray();

        return constructor.Invoke(args);
    }

    private object ResolveConstructorParameter(Type paramType)
    {
        if (paramType.IsArray)
            return CreateSingletonArray(paramType.GetElementType()!);

        if (IsCollectionType(paramType))
            return CreateSingletonList(paramType.GetGenericArguments()[0]);

        return Resolve(paramType);
    }

    private object CreateSingletonArray(Type elementType)
    {
        var items = ResolveAll(elementType).ToList();
        var array = Array.CreateInstance(elementType, items.Count);
        for (var i = 0; i < items.Count; i++)
            array.SetValue(items[i], i);
        return array;
    }

    private object CreateSingletonList(Type elementType)
    {
        var items = ResolveAll(elementType);
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (IList)Activator.CreateInstance(listType)!;
        foreach (var item in items)
            list.Add(item);
        return list;
    }
}
