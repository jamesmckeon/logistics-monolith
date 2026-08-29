using Throughline.Common.Models;

namespace Throughline.Modules.Ordering.Domain;

public sealed class OrderLine : ValueObject
{
    internal OrderLine(SkuCode skuCode, int quantity)
    {
        SkuCode = skuCode;
        Quantity = quantity;
    }

    public SkuCode SkuCode { get; }
    public int Quantity { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return SkuCode;
    }
}