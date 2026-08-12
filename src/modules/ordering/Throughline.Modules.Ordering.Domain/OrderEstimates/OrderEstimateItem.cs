using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.Pricing;

namespace Throughline.Modules.Ordering.Domain.OrderEstimates;

public sealed record OrderEstimateItem
{
    internal OrderEstimateItem(
        CaseInsensitiveString skuCode,
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

    public CaseInsensitiveString SkuCode { get; }
    public int Quantity { get; }
    public Rate PickFeeRate { get; }
    public Money TotalPickFee { get; }
    public decimal Weight { get; }
    public Money TotalHandling { get; }
}