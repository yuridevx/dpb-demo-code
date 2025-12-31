namespace GoBo.Infrastructure.Logic;

internal sealed class BranchContext : IBranchContext
{
    private readonly Queue<Type> _nestedLogic = new();
    public HashSet<Type> CollectedTypes { get; } = [];

    public void Use<T>() where T : IComponent
    {
        var type = typeof(T);

        if (typeof(IBranch).IsAssignableFrom(type))
            // Queue for recursive evaluation
            _nestedLogic.Enqueue(type);
        else if (typeof(IFlow).IsAssignableFrom(type))
            // Add to active set
            CollectedTypes.Add(type);
    }

    public Type? TakeNestedLogic()
    {
        return _nestedLogic.Count > 0 ? _nestedLogic.Dequeue() : null;
    }

    /// <summary>
    ///     Creates a snapshot by copying the current state.
    ///     Used to undo declarations if a branch throws an exception.
    /// </summary>
    public BranchContextSnapshot Snapshot()
    {
        return new BranchContextSnapshot(
            new HashSet<Type>(CollectedTypes),
            new Queue<Type>(_nestedLogic));
    }

    /// <summary>
    ///     Rolls back to a previous snapshot by restoring the copied state.
    /// </summary>
    public void Rollback(BranchContextSnapshot snapshot)
    {
        CollectedTypes.Clear();
        foreach (var type in snapshot.CollectedTypes)
            CollectedTypes.Add(type);

        _nestedLogic.Clear();
        foreach (var type in snapshot.NestedLogic)
            _nestedLogic.Enqueue(type);
    }
}

/// <summary>
///     Immutable snapshot of BranchContext state for transactional rollback.
/// </summary>
internal sealed class BranchContextSnapshot(HashSet<Type> collectedTypes, Queue<Type> nestedLogic)
{
    public HashSet<Type> CollectedTypes { get; } = collectedTypes;
    public Queue<Type> NestedLogic { get; } = nestedLogic;
}
