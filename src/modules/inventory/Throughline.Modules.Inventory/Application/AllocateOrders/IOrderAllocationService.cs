using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Allocation;

namespace Throughline.Modules.Inventory.Application.AllocateOrders;

internal interface IOrderAllocationService
{
    /// <param name="callerId">The Entra object ID (<c>oid</c>) of the caller allocating the order</param>
    Task<AllocatedOrder> AllocateOrderAsync(
        int ownerId,
        Guid callerId,
        Guid orderId,
        AllocationPolicies policy,
        CancellationToken token);
}