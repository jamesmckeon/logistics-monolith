using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Common;

namespace Throughline.Modules.Inventory.Domain.Allocation;

/// <param name="CreatedBy">
///     The Entra object ID (<c>oid</c>) of the caller who allocated it; null only for allocations made before it was
///     recorded
/// </param>
internal sealed record ReceiptAllocation(
    EntityId SkuReceiptId,
    int QuantityAllocated,
    AppDateTime AllocatedOn,
    Guid? CreatedBy)
{
}