namespace Throughline.Modules.Ordering.Domain.Models;

public sealed class Rate : ValueObject
{
    public Rate(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
        Value = value;
    }

    public decimal Value { get; }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }
}