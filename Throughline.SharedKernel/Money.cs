namespace Throughline.SharedKernel;

public sealed class Money : ValueObject
{
    public decimal Value { get; }

    public Money(decimal value)
    {
        Value = Math.Round(value, 2, MidpointRounding.ToEven);
    }
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }
}