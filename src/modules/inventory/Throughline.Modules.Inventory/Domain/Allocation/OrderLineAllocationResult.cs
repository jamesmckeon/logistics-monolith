namespace Throughline.Modules.Inventory.Domain.Allocation;

internal sealed record OrderLineAllocationResult(
    OrderLineAllocation OrderLine,
    IEnumerable<InventoryAllocation> InventoryAllocations,
    int QuantityAllocated,
    AllocationStatus AllocationStatus
)
{
    public bool AllocatedQuantityChanged => OrderLine.QuantityRequested != QuantityAllocated;
}