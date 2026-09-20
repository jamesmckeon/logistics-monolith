using Throughline.Modules.Inventory.Domain.Orders;

namespace Throughline.Modules.Inventory.Domain.Allocation;

public interface IOrderAllocationsRepository
{
    Task SaveConfirmedOrder(
        Order order, CancellationToken token);

    Task<Order?> GetByOwnerOrderIdAsync(OwnerOrderId id, CancellationToken token);
}