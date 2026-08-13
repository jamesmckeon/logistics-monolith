using Throughline.Modules.Ordering.Domain.Models;

namespace Throughline.Modules.Ordering.Domain.OrderEstimates;

public sealed class OrderEstimateRequestItem
{
    internal OrderEstimateRequestItem(
        SkuCode skuCode, int totalQuantity, decimal unitWeight, Rate unitPickFee)
    {
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