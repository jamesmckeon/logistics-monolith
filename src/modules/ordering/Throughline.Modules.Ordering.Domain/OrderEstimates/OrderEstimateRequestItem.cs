using Throughline.Modules.Ordering.Domain.Models;

namespace Throughline.Modules.Ordering.Domain.OrderEstimates;

public sealed class OrderEstimateRequestItem
{
    public OrderEstimateRequestItem(CaseInsensitiveString skuCode, int quantity)
    {
        ArgumentNullException.ThrowIfNull(skuCode);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 0);

        SkuCode = skuCode;
        Quantity = quantity;
    }

    public CaseInsensitiveString SkuCode { get; }
    public int Quantity { get; }
}