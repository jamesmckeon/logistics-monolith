namespace Throughline.Common.Models;

/// <summary>
///     Two entities are equal when they share the same <see cref="Id" />, regardless of any mutable state
/// </summary>
/// <typeparam name="TId">The identity type (itself typically a value object).</typeparam>
public abstract class Entity<TId> : ValueObject
    where TId : notnull
{
    protected Entity(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
    }

    public TId Id { get; }

    protected sealed override IEnumerable<object?> GetAtomicValues()
    {
        yield return Id;
    }
}