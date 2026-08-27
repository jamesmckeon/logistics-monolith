using Throughline.Modules.Billing.Domain.Models;
using Throughline.Modules.Billing.Domain.Pricing;

namespace Throughline.Modules.Billing.Domain.OrderEstimates;

public sealed record OrderEstimateItem
{
    internal OrderEstimateItem(
        SkuCode skuCode,
        int quantity,
        Rate pickFeeRate,
        Money totalPickFee,
        decimal weight,
        Money totalHandling
    )
    {
        SkuCode = skuCode;
        Quantity = quantity;
        PickFeeRate = pickFeeRate;
        TotalPickFee = totalPickFee;
        Weight = weight;
        TotalHandling = totalHandling;
    }

    public SkuCode SkuCode { get; }
    public int Quantity { get; }
    public Rate PickFeeRate { get; }
    public Money TotalPickFee { get; }
    public decimal Weight { get; }
    public Money TotalHandling { get; }
}