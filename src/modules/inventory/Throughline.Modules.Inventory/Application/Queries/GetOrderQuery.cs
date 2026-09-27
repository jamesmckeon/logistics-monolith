using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Inventory;

namespace Throughline.Modules.Inventory.Application.Queries;

internal sealed class GetOrderQuery
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IOrderAllocationRepository _orderAllocationRepository;

    public GetOrderQuery(
        IOrderAllocationRepository orderAllocationRepository,
        IInventoryRepository inventoryRepository)
    {
        _orderAllocationRepository = orderAllocationRepository;
        _inventoryRepository = inventoryRepository;
    }

    public async Task<OrderAllocationModel?> GetOrderByIdAsync(int ownerId, Guid orderId, CancellationToken token)
    {
        var order = await _orderAllocationRepository.GetByOrderIdAsync(orderId, token);

        // Another owner's order is reported as not found, so callers can't probe for order ids they don't own.
        if (order is null || order.OwnerId != ownerId)
            return null;

        var skus = await _inventoryRepository.GetSkusByIdAsync(
            order.OrderLines.Select(l => l.SkuId).Distinct(), token);
        var skuCodes = skus.ToDictionary(s => s.Id.Value, s => s.Code);

        var lines = order.OrderLines
            .Select(l => new OrderLineAllocationModel(
                skuCodes[l.SkuId.Value],
                l.QuantityRequested,
                l.QuantityAllocated,
                l.AllocationStatus.ToString()))
            .ToList()
            .AsReadOnly();

        return new OrderAllocationModel(
            order.OwnerId, order.Id, order.AllocationStatus.ToString(), lines);
    }
}