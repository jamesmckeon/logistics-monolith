using Throughline.Common.Events;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Infrastructure.Db;
using Throughline.Modules.Ordering.Contracts.Events;
using Wolverine.Attributes;

namespace Throughline.Modules.Inventory.Infrastructure.Events;

[Transactional(typeof(InventoryDbContext))]
public sealed class OrderConfirmedHandler
{
    public async Task Handle(
        OrderConfirmedIntegrationEvent message,
        InventoryDbContext db,
        ILogger<OrderConfirmedHandler> logger,
        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(db);

        IInventoryRepository inventoryRepository = db;
        IOrderAllocationRepository orderAllocationRepository = db;

        logger.LogInformation(
            "Received OrderConfirmedIntegrationEVent for owner {OwnerId}, order {OrderId}",
            message.OwnerId, message.OrderId);

        var messageSkus = message.Lines.Select(l => l.SkuCode)
            .ToList();
        var validSkus = await inventoryRepository.GetSkusByOwnerIdAsync(
            message.OwnerId, messageSkus, token);

        try
        {
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
                throw Poison(message, logger, lineErrors);

            var orderResult =
                // null forgiving is ok here bc we verified that all results succeeded, so all results
                // must have a value
                OrderAllocation.Create(
                    message.OwnerId, message.OrderId, lineResults.Select(r => r.Value!));

            if (!orderResult.Succeeded)
                throw Poison(message, logger, orderResult.Errors.Select(e => e.Description));

            orderAllocationRepository.Add(orderResult.Value);

            logger.LogInformation(
                "Added new confirmed order for owner {OwnerId}, order {OrderId}; committed by the transactional middleware",
                message.OwnerId, message.OrderId);
        }
        catch (Exception ex)
        {
            logger.LogError("OrderConfirmedIntegrationEvent handling encountered an error", ex);
            throw;
        }
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