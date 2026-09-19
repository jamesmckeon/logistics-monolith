using Throughline.Common.Models;

namespace Throughline.Modules.Inventory.Orders;

public sealed class OrderLine : ValueObject
{
    public OrderLine(string skuCode, int quantityRequested)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skuCode);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantityRequested);

        SkuCode = skuCode;
        QuantityRequested = quantityRequested;
    }

    public string SkuCode { get; }
    public int QuantityRequested { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return SkuCode;
        yield return QuantityRequested;
    }
}