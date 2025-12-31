using System.Reflection;

namespace GoBo.Infrastructure.Modules;

/// <summary>
///     Holds metadata about a registered singleton type.
/// </summary>
public sealed class SingletonDef(Type type)
{
    public Type Type { get; } = type;
    public int Priority { get; internal set; }
    public ConstructorInfo Constructor { get; internal set; }
    public IFactory Factory { get; internal set; }
    public object Instance { get; internal set; }
}
