using Throughline.Modules.Inventory.Domain.Receiving;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal sealed record OrderLineAllocationResult(
    OrderLineAllocation OrderLine,
    IEnumerable<InventoryPallet> SkuReceipts
)
{
}