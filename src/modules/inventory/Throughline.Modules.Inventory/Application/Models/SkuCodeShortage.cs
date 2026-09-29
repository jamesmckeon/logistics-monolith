namespace Throughline.Modules.Inventory.Application.Models;

public sealed record SkuCodeShortage(
    string Sku,
    int QuantityRequested,
    int QuantityAllocated,
    int QuantityOutstanding)
{
}