namespace Throughline.Modules.Inventory.Application.Models;

internal sealed record OrderLineAllocationModel(
    string SkuCode,
    int QuantityRequested,
    int QuantityAllocated,
    string AllocationStatus);