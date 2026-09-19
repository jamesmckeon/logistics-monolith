namespace Throughline.Modules.Inventory.Orders;

public interface IOrdersRepository
{
    Task SaveConfirmedOrder(
        Order order, CancellationToken token);

    Task<Order?> GetByOwnerOrderIdAsync(OwnerOrderId id, CancellationToken token);
}