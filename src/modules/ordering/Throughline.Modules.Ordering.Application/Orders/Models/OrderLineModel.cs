namespace Throughline.Modules.Ordering.Application.Orders.Models;

public sealed record OrderLineModel(
    string SkuCode,
    int Quantity,
    decimal UnitPickFee,
    decimal TotalPickFees,
    decimal TotalWeight,
    decimal TotalHandling)
{
}