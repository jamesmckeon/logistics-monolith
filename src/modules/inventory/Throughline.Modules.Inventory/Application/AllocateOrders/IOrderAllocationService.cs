using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Allocation;

namespace Throughline.Modules.Inventory.Application.AllocateOrders;

internal interface IOrderAllocationService
{
    Task<AllocatedOrder> AllocateOrderAsync(
        OrderAllocation order,
        AllocationPolicies policy,
        CancellationToken token);
}