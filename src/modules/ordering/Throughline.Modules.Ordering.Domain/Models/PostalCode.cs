namespace Throughline.Modules.Ordering.Domain.Models;

public sealed class PostalCode : ValueObject
{
    internal const int MinValue = 501;
    internal const int MaxValue = 99950;

    public PostalCode(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(MinValue, value, nameof(value));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(MaxValue, value, nameof(value));
        Value = value;
    }

    public int Value { get; }

    protected override IEnumerable<object> GetAtomicValues()
    {
        throw new NotImplementedException();
    }

    public static bool operator >=(PostalCode left, PostalCode right)
    {
        if (left is null) return false;
        if (right is null) return true;

        return left.Value >= right.Value;
    }

    public static bool operator <=(PostalCode left, PostalCode right)
    {
        if (left is null) return true;
        if (right is null) return false;

        return left.Value <= right.Value;
    }

    public static bool operator >(PostalCode left, PostalCode right)
    {
        if (left is null) return false;
        if (right is null) return true;

        return left.Value > right.Value;
    }

    public static bool operator <(PostalCode left, PostalCode right)
    {
        if (left is null) return true;
        if (right is null) return false;

        return left.Value < right.Value;
    }

    public override string ToString()
    {
        return Value.ToString().PadLeft(5, '0');
    }
}