using Throughline.Common.Models;

namespace Throughline.Modules.Ordering.Domain;

internal sealed class SkuCode : ValueObject
{
    internal SkuCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim().ToUpperInvariant();
    }

    public string Value { get; }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }
}