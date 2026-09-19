using Throughline.Common.Models;

namespace Throughline.Modules.Inventory.Domain.Skus;

internal sealed class SkuCode : ValueObject
{
    public string Value { get; }

    public SkuCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        Value = value.Trim().ToUpperInvariant();
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }
}