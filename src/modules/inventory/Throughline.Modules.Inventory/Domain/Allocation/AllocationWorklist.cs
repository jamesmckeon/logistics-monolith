namespace Throughline.Modules.Inventory.Domain.Allocation;

/// <summary>
///     The two lists of an owner's orders still to allocate. An order is released once an allocation attempt
///     has started on it (<see cref="OrderAllocation.StartAllocating" />), whatever the attempt's outcome, and
///     stays released. An order is on at most one list: neither holds fully allocated orders or orders being
///     allocated.
/// </summary>
internal enum AllocationWorklist
{
    /// <summary>
    ///     Orders never released to allocation: no attempt has started on them
    /// </summary>
    Unreleased,

    /// <summary>
    ///     Orders released to allocation that still aren't fully allocated, whether stock was short, the owner's
    ///     policy refused the allocation, or the attempt failed
    /// </summary>
    Backordered
}
