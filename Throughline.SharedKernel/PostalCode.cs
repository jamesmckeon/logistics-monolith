namespace Throughline.SharedKernel;

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
}