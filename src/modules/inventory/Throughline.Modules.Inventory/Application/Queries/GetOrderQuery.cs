using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Orders;

namespace Throughline.Modules.Inventory.Application.Queries;

internal sealed class GetOrderQuery
{
    private readonly IOrderAllocationsRepository _orderAllocationsRepository;

    public GetOrderQuery(IOrderAllocationsRepository orderAllocationsRepository)
    {
        _orderAllocationsRepository = orderAllocationsRepository;
    }

    public async Task<OrderAllocationModel?> GetOrderByOwnerOrderIdAsync(int ownerId, Guid orderId,
        CancellationToken token)
    {
        var order = await _orderAllocationsRepository.GetByOwnerOrderIdAsync(new OwnerOrderId(ownerId, orderId), token);

        return order == null ? null : OrderAllocationModel.FromOrder(order);
    }
}