using Throughline.Modules.Inventory.Domain.Common;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal sealed record SkuIdShortage(
    EntityId SkuId,
    int QuantityRequested,
    int QuantityAllocated,
    int QuantityOutstanding)
{
}