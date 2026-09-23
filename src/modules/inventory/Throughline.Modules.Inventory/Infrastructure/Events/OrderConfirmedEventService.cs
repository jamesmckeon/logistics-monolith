using Throughline.Common.Events;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Infrastructure.Common;
using Throughline.Modules.Ordering.Contracts.Events;

namespace Throughline.Modules.Inventory.Infrastructure.Events;

internal class OrderConfirmedEventService : IOrderConfirmedEventService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<OrderConfirmedEventService> _logger;
    private readonly IOrderAllocationRepository _orderAllocationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OrderConfirmedEventService(
        IOrderAllocationRepository orderAllocationRepository,
        IInventoryRepository inventoryRepository,
        ILogger<OrderConfirmedEventService> logger,
        IUnitOfWork unitOfWork)
    {
        _orderAllocationRepository = orderAllocationRepository;
        _inventoryRepository = inventoryRepository;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleMessageAsync(OrderConfirmedIntegrationEvent message, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(message);

        _logger.LogInformation(
            "Received OrderConfirmed for owner {OwnerId}, order {OrderId}",
            message.OwnerId, message.OrderId);

        var messageSkus = message.Lines.Select(l => l.SkuCode)
            .ToList();
        var validSkus = await _inventoryRepository.GetSkusByOwnerIdAsync(message.OwnerId,
            messageSkus);

        if (validSkus.Count != messageSkus.Count)
        {
            var invalidSkus = messageSkus.Except(validSkus.Select(s => s.Code));
            throw new MissingDependencyException(
                $"The following skus were not found: {string.Join(", ", invalidSkus)}");
        }

        var lineResults = message.Lines.Select(l =>
                OrderLineAllocation.Create(
                    EntityId.Create(),
                    validSkus.Single(s => s.Code == l.SkuCode).Id,
                    l.QuantityRequested))
            .ToList();

        // permanent (poison) failure; retrying the same data can't succeed, so dead-letter it.
        var lineErrors = lineResults
            .Where(r => !r.Succeeded)
            .SelectMany(r => r.Errors)
            .Select(e => e.Description)
            .ToList();

        if (lineErrors.Count > 0)
            throw Poison(message, _logger, lineErrors);

        var orderResult =
            // null forgiving is ok here bc we verified that all results succeeded, so all results
            // must have a value
            OrderAllocation.Create(message.OwnerId, message.OrderId, lineResults.Select(r => r.Value!));

        if (!orderResult.Succeeded)
            throw Poison(message, _logger, orderResult.Errors.Select(e => e.Description));

        _orderAllocationRepository.Add(orderResult.Value);

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Saved new confirmed order for owner {OwnerId}, order {OrderId}",
            message.OwnerId, message.OrderId);
    }

    private static UnrecoverableMessageException Poison(
        OrderConfirmedIntegrationEvent message,
        ILogger logger,
        IEnumerable<string> errors)
    {
        var reason = string.Join("; ", errors);

        logger.LogError(
            "Invalid OrderConfirmed for owner {OwnerId}, order {OrderId}: {Reason}",
            message.OwnerId, message.OrderId, reason);

        return new UnrecoverableMessageException(
            $"OrderConfirmed for owner {message.OwnerId}, order {message.OrderId} is invalid: {reason}");
    }
}