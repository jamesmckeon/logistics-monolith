using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Inventory;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal sealed class OrderLineAllocationService : IOrderlineAllocationService
{
    public OrderLineAllocationResult AllocateOrderLine(
        OrderLineAllocation orderLine, IEnumerable<SkuReceipt> skuReceipts)
    {
        ArgumentNullException.ThrowIfNull(orderLine);
        ArgumentNullException.ThrowIfNull(skuReceipts);

        if (orderLine.QuantityAllocated == orderLine.QuantityRequested)
            return NotChanged(orderLine);

        if (!skuReceipts.Any())
            return NotChanged(orderLine);

        if (!skuReceipts.Any(a => a.Sku == orderLine.Sku))
            throw new ArgumentException("skuReceipts must contain only receipts for the sku being allocated",
                nameof(skuReceipts));

        var quantity = orderLine.QuantityAllocated;
        var allocations = new List<InventoryAllocation>();

        foreach (var skuReceipt in skuReceipts.OrderByDescending(o => o.ReceivedOn))
        {
            var difference = orderLine.QuantityRequested - quantity;
            var allocated = Math.Min(difference, skuReceipt.QuantityReceived);
            quantity = quantity + allocated;

            allocations.Add(new(skuReceipt, allocated, AppDateTime.Now));
            if (quantity == orderLine.QuantityRequested)
                break;
        }

        var status = quantity == orderLine.QuantityRequested
            ? AllocationStatus.Allocated
            : AllocationStatus.PartiallyAllocated;

        return new(orderLine, allocations, quantity, status);
    }

    private static OrderLineAllocationResult NotChanged(OrderLineAllocation orderLine)
    {
        return new OrderLineAllocationResult(
            orderLine, [], orderLine.QuantityAllocated, orderLine.AllocationStatus);
    }
}