using Throughline.Modules.Ordering.Domain.Models;

namespace Throughline.Modules.Ordering.Domain.Pricing;

public sealed class Money : ValueObject
{
    public Money(decimal value)
    {
        Value = Math.Round(value, 2, MidpointRounding.ToEven);
    }

    public decimal Value { get; }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public static Money FromRate(int multiplier, Rate rate)
    {
        return new Money(multiplier * rate.Value);
    }

    public static Money FromRate(decimal multiplier, Rate rate)
    {
        return new Money(multiplier * rate.Value);
    }

    public static Money operator +(Money left, Money right)
    {
        return new Money(left.Value + right.Value);
    }
}