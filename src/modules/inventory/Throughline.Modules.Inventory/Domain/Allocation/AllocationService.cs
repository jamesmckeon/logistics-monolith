using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Inventory;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal sealed class AllocationService : IAllocationService
{
    public void AllocateOrderLine(
        Guid orderId,
        OrderLineAllocation orderLine,
        IEnumerable<SkuReceipt> skuReceipts)
    {
        ArgumentNullException.ThrowIfNull(orderLine);
        ArgumentNullException.ThrowIfNull(skuReceipts);

        if (orderLine.AllocationStatus == AllocationStatus.Allocated)
            return;

        var receipts = skuReceipts.ToArray();

        if (!receipts.Any())
            return;

        if (receipts.All(a => a.SkuId != orderLine.SkuId))
            throw new ArgumentException("skuReceipts must contain only receipts for the sku being allocated",
                nameof(skuReceipts));

        foreach (var skuReceipt in receipts.OrderByDescending(o => o.ReceivedOn))
        {
            if (skuReceipt.QuantityAvailable >= orderLine.QuantityShort)
            {
                var quantityAllocated = skuReceipt.AllocateToOrder(
                    orderId, orderLine.QuantityShort, AppDateTime.Now);

                if (quantityAllocated > 0)
                    orderLine.IncreaseQuantityAllocated(quantityAllocated, AppDateTime.Now);
            }

            if (orderLine.QuantityShort == 0)
                break;
        }
    }

    public bool CanSatisfyPolicyWithCurrentReceipts(OrderAllocation order, IEnumerable<SkuReceipt> receipts,
        AllocationPolicies policy)
    {
        if (order.AllocationStatus == AllocationStatus.Allocated)
            return true;

        if (policy == AllocationPolicies.Partial)
            return true;

        var receiptsArray = receipts.ToArray();
        if (!receiptsArray.Any())
            return false;

        var unallocatedLines = order.UnallocatedLines;

        var skuQuantities = receiptsArray.Where(r => unallocatedLines.Select(ul => ul.SkuId).Contains(r.SkuId))
            .GroupBy(r => r.SkuId)
            .Select(grp => new
            {
                SkuId = grp.Key,
                QuantityAvailable = grp.Sum(sr => sr.QuantityAvailable)
            }).ToArray();

        // at this point, we're only dealing with the "ship complete" policy
        if (!skuQuantities.Any())
            return false;

        return unallocatedLines.All(all =>
            skuQuantities.Single(s => s.SkuId == all.SkuId).QuantityAvailable >= all.QuantityShort);
    }
}