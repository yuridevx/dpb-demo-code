namespace GoBo.Infrastructure.Modules;

/// <summary>
///     Lightweight dependency injection container interface.
///     Singletons are resolved via Resolve&lt;T&gt;(), prototypes via New&lt;T&gt;().
/// </summary>
public interface IScope
{
    // === Type-based API ===

    /// <summary>
    ///     Resolves a single instance of the specified type.
    ///     Throws if no type or multiple types match.
    /// </summary>
    object Resolve(Type type);

    /// <summary>
    ///     Resolves all instances assignable to the specified type.
    /// </summary>
    IEnumerable<object> ResolveAll(Type type);

    /// <summary>
    ///     Gets all registered singleton types.
    /// </summary>
    IEnumerable<Type> GetRegisteredSingletonTypes();

    /// <summary>
    ///     Gets all registered singleton types assignable to the specified type.
    /// </summary>
    IEnumerable<Type> GetRegisteredSingletonTypes(Type assignableTo);

    /// <summary>
    ///     Gets the single registered singleton type assignable to the specified type.
    ///     Throws if no type or multiple types match.
    /// </summary>
    Type GetRegisteredSingletonType(Type assignableTo);

    /// <summary>
    ///     Gets all registered prototype types.
    /// </summary>
    IEnumerable<Type> GetRegisteredPrototypeTypes();

    /// <summary>
    ///     Gets all registered prototype types assignable to the specified type.
    /// </summary>
    IEnumerable<Type> GetRegisteredPrototypeTypes(Type assignableTo);

    /// <summary>
    ///     Gets the single registered prototype type assignable to the specified type.
    ///     Throws if no type or multiple types match.
    /// </summary>
    Type GetRegisteredPrototypeType(Type assignableTo);

    /// <summary>
    ///     Checks if a type can be resolved.
    /// </summary>
    bool CanResolve(Type type);

    // === Generic API ===

    /// <summary>
    ///     Resolves a single instance of type T.
    /// </summary>
    T Resolve<T>();

    /// <summary>
    ///     Resolves all instances assignable to type T.
    /// </summary>
    IEnumerable<T> ResolveAll<T>();

    /// <summary>
    ///     Gets all registered singleton types assignable to type T.
    /// </summary>
    IEnumerable<Type> GetRegisteredSingletonTypes<T>();

    /// <summary>
    ///     Gets the single registered singleton type assignable to type T.
    ///     Throws if no type or multiple types match.
    /// </summary>
    Type GetRegisteredSingletonType<T>();

    /// <summary>
    ///     Gets all registered prototype types assignable to type T.
    /// </summary>
    IEnumerable<Type> GetRegisteredPrototypeTypes<T>();

    /// <summary>
    ///     Gets the single registered prototype type assignable to type T.
    ///     Throws if no type or multiple types match.
    /// </summary>
    Type GetRegisteredPrototypeType<T>();

    /// <summary>
    ///     Checks if type T can be resolved.
    /// </summary>
    bool CanResolve<T>();

    // === Prototype API (for [Prototype] types) ===

    /// <summary>
    ///     Creates a new instance of a prototype type.
    ///     Throws if the type is not registered as a prototype.
    /// </summary>
    object New(Type type);

    /// <summary>
    ///     Creates a new instance of prototype type T.
    ///     Throws if T is not registered as a prototype.
    /// </summary>
    T New<T>();

    /// <summary>
    ///     Checks if a type can be created via New().
    ///     Returns true for registered prototypes and unregistered types with satisfiable constructors.
    ///     Returns false for singletons and types with unsatisfiable constructors.
    /// </summary>
    bool CanNew(Type type);

    /// <summary>
    ///     Checks if type T can be created via New().
    ///     Returns true for registered prototypes and unregistered types with satisfiable constructors.
    ///     Returns false for singletons and types with unsatisfiable constructors.
    /// </summary>
    bool CanNew<T>();
}