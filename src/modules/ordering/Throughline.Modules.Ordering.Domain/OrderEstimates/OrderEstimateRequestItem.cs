using Throughline.Modules.Ordering.Domain.Models;

namespace Throughline.Modules.Ordering.Domain.OrderEstimates;

public sealed class OrderEstimateRequestItem
{
    public OrderEstimateRequestItem(
        CaseInsensitiveString skuCode,
        int quantity,
        decimal weight)
    {
        ArgumentNullException.ThrowIfNull(skuCode);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(weight, 0);

        SkuCode = skuCode;
        Quantity = quantity;
        Weight = weight;
    }

    public CaseInsensitiveString SkuCode { get; }
    public int Quantity { get; }
    public decimal Weight { get; }
}