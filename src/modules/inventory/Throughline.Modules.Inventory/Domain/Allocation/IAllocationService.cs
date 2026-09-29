using Throughline.Modules.Inventory.Domain.Receiving;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal interface IAllocationService
{
    void AllocateOrderLine(
        Guid orderId,
        OrderLineAllocation orderLine,
        IEnumerable<InventoryPallet> skuReceipts);

    /// <summary>
    ///     Returns true if an owner's <c>AllocationPolicy</c> can be satisfied with a set of SkuReceipts
    /// </summary>
    bool CanSatisfyPolicyWithCurrentReceipts(
        OrderAllocation order,
        IEnumerable<InventoryPallet> receipts,
        AllocationPolicies policy);

    /// <summary>
    ///     Returns the skus on <paramref name="order" /> that would remain partially allocated after quantities
    ///     from <paramref name="receipts" /> were applied
    /// </summary>
    IEnumerable<SkuIdShortage> GetShortedSkus(OrderAllocation order,
        IEnumerable<InventoryPallet> receipts);

    /// <summary>
    ///     Derives the <c>AllocationStatus</c> that would result from applying
    ///     <paramref name="shortages" /> to the order's unallocated lines.
    /// </summary>
    /// <remarks>
    ///     Used when allocation is aborted by policy but the caller still needs to know the status
    ///     the order would have after applying the identified shortages. This allows the response
    ///     to distinguish between a partially allocated order and one that remains confirmed/unallocated.
    /// </remarks>
    AllocationStatuses DeriveStatusFromShortages(OrderAllocation order, IEnumerable<SkuIdShortage> shortages);
}