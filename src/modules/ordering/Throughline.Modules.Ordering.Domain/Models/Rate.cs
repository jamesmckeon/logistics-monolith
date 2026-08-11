namespace Throughline.Modules.Ordering.Domain.Models;

public sealed class Rate : ValueObject
{
    public Rate(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(0, value, nameof(value));
        Value = value;
    }

    public decimal Value { get; }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }
}