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
}