using Throughline.Modules.Billing.Domain.Models;
using Throughline.Modules.Billing.Domain.Pricing;

namespace Throughline.Modules.Billing.Domain.Orders;

public sealed class OrderLine
{
    internal OrderLine(
        int totalQuantity,
        SkuCode skuCode,
        Rate unitPickFee,
        Money totalPickFees,
        decimal totalWeight,
        Money totalHandling)
    {
        SkuCode = skuCode;
        TotalQuantity = totalQuantity;
        UnitPickFee = unitPickFee;
        TotalPickFees = totalPickFees;
        TotalWeight = totalWeight;
        TotalHandling = totalHandling;
    }

    public int TotalQuantity { get; }
    public SkuCode SkuCode { get; }
    public Rate UnitPickFee { get; }
    public Money TotalPickFees { get; }
    public decimal TotalWeight { get; }
    public Money TotalHandling { get; }
}