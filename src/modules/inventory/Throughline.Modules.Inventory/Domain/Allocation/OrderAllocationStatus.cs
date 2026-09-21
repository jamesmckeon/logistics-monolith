namespace Throughline.Modules.Inventory.Domain.Allocation;

public enum OrderAllocationStatus
{
    /// <summary>
    ///     An order's initial state, after the customer has sent an EDI 940
    /// </summary>
    Confirmed,

    /// <summary>
    ///     Allocation started but inventory was insufficient for all lines on the order
    /// </summary>
    NotAllocated,

    /// <summary>
    ///     Some of the lines on the order were at least partially allocated
    /// </summary>
    PartiallyAllocated,

    /// <summary>
    ///     All lines on the order were fully allocated
    /// </summary>
    Allocated,

    /// <summary>
    ///     The order is currently being allocated
    /// </summary>
    Allocating
}