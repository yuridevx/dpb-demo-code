namespace GoBo.Infrastructure.Modules;

public sealed partial class Scope
{
    private static readonly HashSet<Type> CollectionTypes =
    [
        typeof(List<>),
        typeof(IList<>),
        typeof(ICollection<>),
        typeof(IEnumerable<>),
        typeof(IReadOnlyCollection<>),
        typeof(IReadOnlyList<>)
    ];

    private static bool IsCollectionType(Type type)
    {
        return type.IsArray || (type.IsGenericType && CollectionTypes.Contains(type.GetGenericTypeDefinition()));
    }

    private static Type GetElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType()!;

        if (type.IsGenericType && CollectionTypes.Contains(type.GetGenericTypeDefinition()))
            return type.GetGenericArguments()[0];

        return type;
    }
}