using Throughline.Common.Results;
using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Owners;

namespace Throughline.Modules.Inventory.Application.AllocateOrders;

public sealed class AllocateOrdersHandler
{
    private readonly ILogger<AllocateOrdersHandler> _logger;
    private readonly OrderAllocationService _orderAllocationService;
    private readonly IOrderAllocationRepository _orderRepository;
    private readonly IOwnerProvider _ownerProvider;
    private readonly AllocationSpecContext _specContext;

    internal AllocateOrdersHandler(
        ILogger<AllocateOrdersHandler> logger,
        OrderAllocationService orderAllocationService,
        IOrderAllocationRepository orderAllocationRepository,
        IOwnerProvider ownerProvider,
        AllocationSpecContext allocationSpecContext)
    {
        _logger = logger;
        _orderAllocationService = orderAllocationService;
        _orderRepository = orderAllocationRepository;
        _ownerProvider = ownerProvider;
        _specContext = allocationSpecContext;
    }

    public async Task<AllocateOrdersResult> AllocateOrdersAsync(
        AllocateOrdersCommand command, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!command.OrderIds.Any())
            throw new InvalidOperationException("command.OrderIds must contain at least one item");

        var orders = await _orderRepository.GetAllByOrderId(command.OrderIds);

        if (!orders.Any())
            return new AllocateOrdersResult(command.OrderIds.Select(AllocationError.OrderNotFound));

        var allocatedOrders = new List<AllocatedOrder>();
        foreach (var order in orders)
        {
            var allocatedOrder = await _orderAllocationService.AllocateOrderAsync(order, token);
            allocatedOrders.Add(allocatedOrder);
        }

        var missingOrders = command.OrderIds.Except(orders.Select(o => o.Id))
            .Select(AllocationError.OrderNotFound);

        return new AllocateOrdersResult(allocatedOrders, missingOrders);
    }

    private static Result<IReadOnlyCollection<AllocatedOrder>> Validation(
        string errorMessage)
    {
        return Result<IReadOnlyCollection<AllocatedOrder>>.Validation(errorMessage);
    }
}