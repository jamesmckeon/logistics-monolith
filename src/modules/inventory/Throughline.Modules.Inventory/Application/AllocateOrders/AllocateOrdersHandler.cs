using Throughline.Common.Results;
using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Owners;

namespace Throughline.Modules.Inventory.Application.AllocateOrders;

internal sealed class AllocateOrdersHandler
{
    private readonly ILogger<AllocateOrdersHandler> _logger;
    private readonly IOrderAllocationService _orderAllocationService;
    private readonly IOrderAllocationRepository _orderRepository;
    private readonly IOwnerProvider _ownerProvider;

    public AllocateOrdersHandler(
        ILogger<AllocateOrdersHandler> logger,
        IOrderAllocationService orderAllocationService,
        IOrderAllocationRepository orderAllocationRepository,
        IOwnerProvider ownerProvider)
    {
        _logger = logger;
        _orderAllocationService = orderAllocationService;
        _orderRepository = orderAllocationRepository;
        _ownerProvider = ownerProvider;
    }

    public async Task<AllocateOrdersResult> AllocateOrdersAsync(
        AllocateOrdersCommand command, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!command.OrderIds.Any())
            throw new ArgumentException(
                "command.OrderIds must contain at least one item",
                nameof(command.OrderIds));

        var orders = await _orderRepository.GetAllByOrderIdAsync(command.OrderIds);
        if (!orders.Any())
            return new AllocateOrdersResult(command.OrderIds.Select(AllocationError.OrderNotFound));

        var ownerIds = orders.Select(o => o.OwnerId).Distinct().ToList();
        if (ownerIds.Count > 1)
            throw new ArgumentException(
                "command.OrderIds must all have the same OwnerId",
                nameof(command.OrderIds));

        var owner = await _ownerProvider.GetOwnerByIdAsync(ownerIds.Single(), token);

        if (owner == null)
            throw new InvalidOperationException($"An owner with id {ownerIds.Single()} wasn't found in the system");

        var allocatedOrders = new List<AllocatedOrder>();
        foreach (var order in orders)
        {
            var allocatedOrder = await _orderAllocationService.AllocateOrderAsync(
                order, owner.AllocationPolicy, token);
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