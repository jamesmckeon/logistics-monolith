using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Orders;

namespace Throughline.Modules.Inventory.Application.Queries;

internal sealed class GetOrderQuery
{
    private readonly IOrdersRepository _ordersRepository;

    public GetOrderQuery(IOrdersRepository ordersRepository)
    {
        _ordersRepository = ordersRepository;
    }

    public async Task<OrderModel?> GetOrderByOwnerOrderIdAsync(int ownerId, Guid orderId, CancellationToken token)
    {
        var order = await _ordersRepository.GetByOwnerOrderIdAsync(new OwnerOrderId(ownerId, orderId), token);

        return order == null ? null : OrderModel.FromOrder(order);
    }
}