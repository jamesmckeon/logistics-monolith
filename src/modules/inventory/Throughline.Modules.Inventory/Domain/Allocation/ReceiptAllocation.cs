using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Common;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal sealed record ReceiptAllocation(
    EntityId SkuReceiptId,
    int QuantityAllocated,
    AppDateTime AllocatedOn)
{
}