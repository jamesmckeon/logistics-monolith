namespace Throughline.Modules.Inventory.Application.Models;

public sealed record AllocationShortage(
    string Sku,
    int QuantityRequested,
    int QuantityAllocated,
    int QuantityOutstanding)
{
}