using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Inventory;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal sealed class OrderLineAllocationService : IOrderlineAllocationService
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
            if (skuReceipt.QuantityAvailable >= orderLine.QuantityUnallocated)
            {
                var quantityAllocated = skuReceipt.AllocateToOrder(
                    orderId, orderLine.QuantityUnallocated, AppDateTime.Now);

                if (quantityAllocated > 0)
                    orderLine.IncreaseQuantityAllocated(quantityAllocated, AppDateTime.Now);
            }

            if (orderLine.QuantityUnallocated == 0)
                break;
        }

        if (orderLine.QuantityUnallocated == 0)
            orderLine.SetAllocated(AppDateTime.Now);
        else
            orderLine.SetPartiallyAllocated(AppDateTime.Now);
    }
}