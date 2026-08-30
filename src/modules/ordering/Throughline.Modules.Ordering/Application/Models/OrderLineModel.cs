namespace Throughline.Modules.Ordering.Application.Models;

public sealed record OrderLineModel(
    string SkuCode,
    int Quantity)
{
}