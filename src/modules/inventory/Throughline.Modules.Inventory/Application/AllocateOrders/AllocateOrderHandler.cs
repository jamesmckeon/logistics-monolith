using Throughline.Common.Results;
using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Owners;

namespace Throughline.Modules.Inventory.Application.AllocateOrders;

public sealed class AllocateOrderHandler
{
    private readonly Logger<AllocateOrderHandler> _logger;
    private readonly OrderAllocationService _orderAllocationService;
    private readonly IOrderAllocationRepository _orderRepository;
    private readonly IOwnerProvider _ownerProvider;
    private readonly AllocationSpecFactory _specFactory;

    public async Task<IEnumerable<AllocatedOrder>> AllocateOrdersAsync(
        AllocateOrdersCommand command, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!command.OrderIds.Any())
            throw new ArgumentException("command.OrderIds must contain at least one item");

        var orders = await _orderRepository.GetAllByOrderId(command.OrderIds);

        if (!orders.Any())
            return command.OrderIds.Select(s => AllocatedOrder.Failed(s, "ORDER_NOT_FOUND"));

        foreach (var order in orders)
        {
            var result = await _orderAllocationService.AllocateOrderAsync(order.Id, token);
            throw new NotImplementedException();
        }
    }

    private static Result<IReadOnlyCollection<AllocatedOrder>> Validation(
        string errorMessage)
    {
        return Result<IReadOnlyCollection<AllocatedOrder>>.Validation(errorMessage);
    }
}