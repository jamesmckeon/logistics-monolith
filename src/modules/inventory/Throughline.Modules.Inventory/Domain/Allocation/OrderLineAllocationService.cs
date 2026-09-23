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
}