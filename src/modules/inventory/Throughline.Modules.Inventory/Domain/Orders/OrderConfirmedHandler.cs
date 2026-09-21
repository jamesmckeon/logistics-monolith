using Microsoft.Extensions.Logging;
using Throughline.Common.Events;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Infrastructure.Common;
using Throughline.Modules.Ordering.Contracts.Events;

namespace Throughline.Modules.Inventory.Domain.Orders;

public sealed class OrderConfirmedHandler
{
    public async Task Handle(
        OrderConfirmedIntegrationEvent message,
        IOrderAllocationRepository orderAllocationRepository,
        ILogger<OrderConfirmedHandler> logger,
        IUnitOfWork unitOfWork,
        CancellationToken token)
    {
        logger.LogInformation(
            "Received OrderConfirmed for owner {OwnerId}, order {OrderId}",
            message.OwnerId, message.OrderId);

        var lineResults = message.Lines.Select(l =>
                OrderLineAllocation.Create(l.SkuCode, l.QuantityRequested))
            .ToList();

        // permanent (poison) failure; retrying the same data can't succeed, so dead-letter it.
        var lineErrors = lineResults
            .Where(r => !r.Succeeded)
            .SelectMany(r => r.Errors)
            .Select(e => e.Description)
            .ToList();

        if (lineErrors.Count > 0)
            throw Poison(message, logger, lineErrors);

        var orderResult =
            // null forgiving is ok here bc we verified that all results succeeded, so all results
            // must have a value
            OrderAllocation.Create(message.OwnerId, message.OrderId, lineResults.Select(r => r.Value!));

        if (!orderResult.Succeeded)
            throw Poison(message, logger, orderResult.Errors.Select(e => e.Description));

        orderAllocationRepository.Add(orderResult.Value);

        await unitOfWork.SaveChangesAsync();

        logger.LogInformation(
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