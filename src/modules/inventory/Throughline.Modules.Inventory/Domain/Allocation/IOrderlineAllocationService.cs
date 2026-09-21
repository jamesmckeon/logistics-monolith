using Throughline.Modules.Inventory.Domain.Inventory;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal interface IOrderlineAllocationService
{
    OrderLineAllocationResult AllocateOrderLine(
        OrderLineAllocation orderLine, IEnumerable<SkuReceipt> skuReceipts);
}