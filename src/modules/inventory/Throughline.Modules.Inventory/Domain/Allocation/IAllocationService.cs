using Throughline.Modules.Inventory.Domain.Inventory;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal interface IAllocationService
{
    void AllocateOrderLine(
        Guid orderId,
        OrderLineAllocation orderLine,
        IEnumerable<SkuReceipt> skuReceipts);

    /// <summary>
    ///     Returns true if an owner's <c>AllocationPolicy</c> can be satisfied with a set of SkuReceipts
    /// </summary>
    bool CanSatisfyPolicyWithCurrentReceipts(
        OrderAllocation order,
        IEnumerable<SkuReceipt> receipts,
        AllocationPolicies policy);

    /// <summary>
    ///     Returns the skus on <paramref name="order" /> that would remain partially allocated after quantities
    ///     from <paramref name="receipts" /> were applied
    /// </summary>
    IEnumerable<SkuIdShortage> GetShortedSkus(OrderAllocation order,
        IEnumerable<SkuReceipt> receipts);

    /// <summary>
    ///     Derives the <c>AllocationStatus</c> that would result from applying
    ///     <paramref name="shortages" /> to the order's unallocated lines.
    /// </summary>
    /// <remarks>
    ///     Used when allocation is aborted by policy but the caller still needs to know the status
    ///     the order would have after applying the identified shortages. This allows the response
    ///     to distinguish between a partially allocated order and one that remains confirmed/unallocated.
    /// </remarks>
    AllocationStatus DeriveStatusFromShortages(OrderAllocation order, IEnumerable<SkuIdShortage> shortages);
}