namespace Throughline.Modules.Inventory.Application.Models;

/// <param name="QuantityToAllocate">Quantity still to allocate; 0 once the line is fully allocated</param>
internal sealed record OrderLineAllocationModel(
    string SkuCode,
    int QuantityRequested,
    int QuantityAllocated,
    int QuantityToAllocate,
    string AllocationStatus);
