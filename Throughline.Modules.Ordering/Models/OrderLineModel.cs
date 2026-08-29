namespace Throughline.Modules.Ordering.Models;

public sealed record OrderLineModel(
    string SkuCode,
    int Quantity)
{
}