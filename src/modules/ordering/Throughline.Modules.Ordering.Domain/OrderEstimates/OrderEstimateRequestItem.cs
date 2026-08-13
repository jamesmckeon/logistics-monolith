using Throughline.Modules.Ordering.Domain.Models;

namespace Throughline.Modules.Ordering.Domain.OrderEstimates;

public sealed class OrderEstimateRequestItem
{
    public OrderEstimateRequestItem(
        SkuCode skuCode, int totalQuantity, decimal unitWeight, Rate unitPickFee)
    {
        ArgumentNullException.ThrowIfNull(skuCode);
        ArgumentNullException.ThrowIfNull(unitPickFee);

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(unitWeight, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(totalQuantity, 0);

        SkuCode = skuCode;
        TotalQuantity = totalQuantity;
        UnitWeight = unitWeight;
        UnitPickFee = unitPickFee;
    }

    public SkuCode SkuCode { get; }
    public int TotalQuantity { get; }
    public decimal UnitWeight { get; }
    public Rate UnitPickFee { get; }
}