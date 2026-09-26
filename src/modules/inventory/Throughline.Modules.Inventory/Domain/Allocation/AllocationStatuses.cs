namespace Throughline.Modules.Inventory.Domain.Allocation;

internal enum AllocationStatuses
{
    /// <summary>
    ///     An order's initial state, after the customer has sent an EDI 940
    /// </summary>
    Confirmed,
    PartiallyAllocated,
    Allocated
}