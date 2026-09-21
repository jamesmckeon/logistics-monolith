using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Allocation;

namespace Throughline.Modules.Inventory.Application.Queries;

internal sealed class GetOrderQuery
{
    private readonly IOrderAllocationRepository _orderAllocationRepository;

    public GetOrderQuery(IOrderAllocationRepository orderAllocationRepository)
    {
        _orderAllocationRepository = orderAllocationRepository;
    }

    public async Task<OrderAllocationModel?> GetOrderByIdAsync(Guid orderId,
        CancellationToken token)
    {
        var order = await _orderAllocationRepository.GetByOrderId(orderId, token);

        return order == null ? null : OrderAllocationModel.FromOrder(order);
    }
}