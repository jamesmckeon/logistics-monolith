namespace Throughline.Common.Models;

/// <summary>
/// Base that derives equality (<see cref="Equals(object?)"/>, <c>==</c>, <c>!=</c> and
/// <see cref="GetHashCode"/>) from a set of components supplied by the derived type via
/// <see cref="GetAtomicValues"/>.
/// <para>
/// A DDD value object yields <b>all</b> of its attributes (value equality). Its subclass
/// <see cref="Entity{TId}"/> yields only its id (identity equality that survives state
/// changes). The name reflects the common case; the mechanism is shared by both.
/// </para>
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    public virtual bool Equals(ValueObject? other)
    {
        return other is not null && ValuesAreEqual(other);
    }

    public static bool operator ==(ValueObject? a, ValueObject? b)
    {
        if (a is null && b is null) return true;

        if (a is null || b is null) return false;

        return a.Equals(b);
    }

    public static bool operator !=(ValueObject? a, ValueObject? b)
    {
        return !(a == b);
    }

    public override bool Equals(object? obj)
    {
        return obj is ValueObject other && ValuesAreEqual(other);
    }

    public override int GetHashCode()
    {
        return GetAtomicValues().Aggregate(
            0,
            (hashcode, value) =>
                HashCode.Combine(hashcode, value?.GetHashCode() ?? 0));
    }

    protected abstract IEnumerable<object?> GetAtomicValues();

    private bool ValuesAreEqual(ValueObject other)
    {
        return GetAtomicValues().SequenceEqual(other.GetAtomicValues());
    }
}
