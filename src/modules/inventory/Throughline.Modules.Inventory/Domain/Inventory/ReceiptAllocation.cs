using Throughline.Common.Models;

namespace Throughline.Modules.Inventory.Domain.Inventory;

internal sealed record ReceiptAllocation(
    Guid OrderId,
    int QuantityAllocated,
    AppDateTime AllocatedOn)
{
}