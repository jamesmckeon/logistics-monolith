using Throughline.Modules.Inventory.Domain.Inventory;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal sealed record OrderLineAllocationResult(
    OrderLineAllocation OrderLine,
    IEnumerable<SkuReceipt> SkuReceipts
)
{
}