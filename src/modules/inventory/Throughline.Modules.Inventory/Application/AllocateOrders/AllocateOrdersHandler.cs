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
        int ownerId,
        AllocateOrdersCommand command,
        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!command.OrderIds.Any())
        {
            _logger.LogInformation("Allocation request rejected as invalid: {ErrorCode}",
                AllocationError.OrderIdsEmptyCode);
            return AllocationError.OrderIdsEmpty("command must contain at least one order id");
        }

        var duplicates = command.OrderIds.GroupBy(grp => grp)
            .Select(grp => new { OrderId = grp.Key, Count = grp.Count() })
            .Where(w => w.Count > 1)
            .ToList();

        if (duplicates.Any())
        {
            _logger.LogInformation("Allocation request rejected as invalid: {ErrorCode}, order ids {OrderIds}",
                AllocationError.DuplicateOrderIdsCode, duplicates.Select(d => d.OrderId));
            return AllocationError.DuplicateOrderIds(duplicates.Select(d => d.OrderId));
        }


        var owner = await _ownerProvider.GetOwnerByIdAsync(ownerId, token);

        if (owner == null)
        {
            throw new InvalidOperationException($"An owner with id {ownerId} wasn't found in the system");
        }

        var allocatedOrders = new List<AllocatedOrder>();

        // as per requirements, orders should be processed in the order they're
        // provided in the request
        foreach (var orderId in command.OrderIds)
        {
            var allocatedOrder = await _orderAllocationService.AllocateOrderAsync(
                ownerId,
                orderId,
                owner.AllocationPolicy,
                token);
            allocatedOrders.Add(allocatedOrder);
        }


        return new AllocateOrdersResult(allocatedOrders);
    }

    private static Result<IReadOnlyCollection<AllocatedOrder>> Validation(
        string errorMessage)
    {
        return Result<IReadOnlyCollection<AllocatedOrder>>.Validation(errorMessage);
    }
}