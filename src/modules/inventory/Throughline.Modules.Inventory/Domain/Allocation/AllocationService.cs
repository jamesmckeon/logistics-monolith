using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Receiving;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal sealed class AllocationService : IAllocationService
{
    public void AllocateOrderLine(
        Guid orderId,
        OrderLineAllocation orderLine,
        IEnumerable<InventoryPallet> skuReceipts)
    {
        ArgumentNullException.ThrowIfNull(orderLine);
        ArgumentNullException.ThrowIfNull(skuReceipts);

        if (orderLine.AllocationStatus == AllocationStatuses.Allocated)
        {
            return;
        }

        // receipts for other skus (e.g. the order's other lines) are ignored; a line with no
        // matching receipts has no stock to allocate from and stays unallocated
        var receipts = skuReceipts.Where(r => r.SkuId == orderLine.SkuId).ToArray();

        if (!receipts.Any())
        {
            return;
        }

        foreach (var skuReceipt in receipts.OrderBy(o => o.ReceivedOn))
        {
            if (orderLine.CanAllocateReceipt(skuReceipt))
            {
                orderLine.AllocateReceipt(skuReceipt, AppDateTime.Now);
            }

            if (orderLine.AllocationStatus == AllocationStatuses.Allocated)
            {
                break;
            }
        }
    }

    public bool CanSatisfyPolicyWithCurrentReceipts(OrderAllocation order, IEnumerable<InventoryPallet> receipts,
        AllocationPolicies policy)
    {
        if (order.AllocationStatus == AllocationStatuses.Allocated)
        {
            return true;
        }

        if (policy == AllocationPolicies.Partial)
        {
            return true;
        }

        var receiptsArray = receipts.ToArray();
        if (!receiptsArray.Any())
        {
            return false;
        }

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
        {
            return false;
        }

        return unallocatedLines.All(all =>
            skuQuantities.Any(a => a.SkuId == all.SkuId) &&
            skuQuantities.Single(s => s.SkuId == all.SkuId).QuantityAvailable >= all.QuantityShort);
    }

    public IEnumerable<SkuIdShortage> GetShortedSkus(
        OrderAllocation order, IEnumerable<InventoryPallet> receipts)
    {
        var shortages = new List<SkuIdShortage>();

        var skuQuantities = receipts.GroupBy(s => s.SkuId)
            .Select(grp => new { SkuId = grp.Key, Quantity = grp.Sum(sm => sm.QuantityAvailable) })
            .ToList();

        foreach (var line in order.UnallocatedLines)
        {
            var availableQuantity = skuQuantities.SingleOrDefault(s => s.SkuId == line.SkuId)?.Quantity ?? 0;
            if (availableQuantity < line.QuantityShort)
            {
                var quantityAllocated = line.QuantityAllocated + availableQuantity;
                shortages.Add(new(line.SkuId, line.QuantityRequested, quantityAllocated,
                    line.QuantityRequested - quantityAllocated));
            }
        }

        return shortages;
    }

    public AllocationStatuses DeriveStatusFromShortages(OrderAllocation order, IEnumerable<SkuIdShortage> shortages)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(shortages);

        if (order.AllocationStatus == AllocationStatuses.Allocated)
        {
            return AllocationStatuses.Allocated;
        }

        var shortagesArray = shortages.ToArray();

        var lineStatuses = order.OrderLines
            .Select(line => DeriveLineStatus(line, shortagesArray))
            .ToArray();

        if (lineStatuses.All(a => a == AllocationStatuses.Confirmed))
        {
            return AllocationStatuses.Confirmed;
        }

        if (lineStatuses.All(a => a == AllocationStatuses.Allocated))
        {
            return AllocationStatuses.Allocated;
        }

        return AllocationStatuses.PartiallyAllocated;
    }

    private static AllocationStatuses DeriveLineStatus(OrderLineAllocation line, SkuIdShortage[] shortages)
    {
        if (!line.IsAllocatable)
        {
            return AllocationStatuses.Allocated;
        }

        // a line without a matching shortage keeps its current allocated quantity
        var quantityAllocated = shortages.SingleOrDefault(s => s.SkuId == line.SkuId)?.QuantityAllocated
                                ?? line.QuantityAllocated;

        if (quantityAllocated > line.QuantityRequested)
        {
            throw new InvalidOperationException("At least one shortage contains a QuantityAllocated that would " +
                                                "exceed a line's QuantityRequested");
        }

        if (quantityAllocated == 0)
        {
            return AllocationStatuses.Confirmed;
        }

        if (quantityAllocated < line.QuantityRequested)
        {
            return AllocationStatuses.PartiallyAllocated;
        }

        return AllocationStatuses.Allocated;
    }
}