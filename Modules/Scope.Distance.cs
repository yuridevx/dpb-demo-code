namespace GoBo.Infrastructure.Modules;

public sealed partial class Scope
{
    private const int InterfacePenalty = 1000;

    /// <summary>
    ///     Calculates the distance from a concrete type to a target type.
    ///     Class inheritance is preferred over interface implementation.
    /// </summary>
    /// <param name="concreteType">The concrete type to measure from.</param>
    /// <param name="targetType">The target type to measure to.</param>
    /// <returns>The distance, or -1 if not assignable.</returns>
    private static int CalculateDistance(Type concreteType, Type targetType)
    {
        if (!targetType.IsAssignableFrom(concreteType))
            return -1;

        var classDistance = GetClassDistance(concreteType, targetType);
        if (classDistance >= 0)
            return classDistance;

        var interfaceDistance = GetInterfaceDistance(concreteType, targetType);
        return interfaceDistance >= 0 ? InterfacePenalty + interfaceDistance : -1;
    }

    private static int GetClassDistance(Type type, Type target)
    {
        if (!target.IsClass) return -1;

        var distance = 0;
        for (var current = type; current != null; current = current.BaseType)
        {
            if (current == target) return distance;
            distance++;
        }

        return -1;
    }

    private static int GetInterfaceDistance(Type type, Type targetInterface)
    {
        if (!targetInterface.IsInterface) return -1;

        var visited = new HashSet<Type>();
        var queue = new Queue<(Type iface, int depth)>();

        foreach (var iface in type.GetInterfaces())
            queue.Enqueue((iface, 0));

        while (queue.Count > 0)
        {
            var (iface, depth) = queue.Dequeue();
            if (!visited.Add(iface)) continue;

            if (iface == targetInterface) return depth;

            foreach (var parent in iface.GetInterfaces())
                queue.Enqueue((parent, depth + 1));
        }

        return -1;
    }
}