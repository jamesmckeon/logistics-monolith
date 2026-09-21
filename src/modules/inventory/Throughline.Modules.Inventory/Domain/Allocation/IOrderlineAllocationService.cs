using Throughline.Modules.Inventory.Domain.Inventory;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal interface IOrderlineAllocationService
{
    void AllocateOrderLine(
        Guid orderId,
        OrderLineAllocation orderLine,
        IEnumerable<SkuReceipt> skuReceipts);
}