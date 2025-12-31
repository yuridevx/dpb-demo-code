namespace GoBo.Infrastructure.Modules;

/// <summary>
///     Factory interface for creating instances of specific types.
///     Factories are discovered and instantiated like any other type.
/// </summary>
public interface IFactory
{
    /// <summary>
    ///     The target type this factory creates instances for.
    ///     Types assignable to this type will use this factory.
    /// </summary>
    Type TargetType { get; }

    /// <summary>
    ///     Creates an instance of the specified concrete type.
    /// </summary>
    /// <param name="scope">The scope for resolving dependencies.</param>
    /// <param name="concreteType">The concrete type to instantiate.</param>
    /// <returns>The created instance.</returns>
    object Create(IScope scope, Type concreteType);
}

/// <summary>
///     Generic base class for factories targeting a specific type.
/// </summary>
/// <typeparam name="TTarget">The target type this factory creates.</typeparam>
public abstract class Factory<TTarget> : IFactory
{
    public Type TargetType => typeof(TTarget);

    public abstract object Create(IScope scope, Type concreteType);
}