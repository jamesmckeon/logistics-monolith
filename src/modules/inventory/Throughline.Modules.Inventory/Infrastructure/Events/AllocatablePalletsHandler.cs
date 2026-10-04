using Throughline.Common.Events;
using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Infrastructure.Db;
using Throughline.Modules.Receiving.Contracts.Events;
using Wolverine.Attributes;

namespace Throughline.Modules.Inventory.Infrastructure.Events;

[Transactional(typeof(InventoryDbContext))]
public sealed class AllocatablePalletsHandler
{
    public async Task Handle(
        AllocatablePalletsIntegrationEvent message,
        InventoryDbContext db,
        ILogger<OrderConfirmedHandler> logger,
        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(db);

        IInventoryRepository inventoryRepository = db;

        // owner on every log record while handling the message
        using var _ = logger.BeginScope("Owner {OwnerId}", message.OwnerId);

        logger.LogInformation(
            "Received AllocatablePalletsIntegrationEvent for owner {OwnerId}, receipt id {OrderId}",
            message.OwnerId, message.ReceiptId);

        var messageSkus = message.Pallets.Select(l => l.SkuCode)
            .ToList();
        var validSkus = await inventoryRepository.GetSkusByOwnerIdAsync(
            message.OwnerId, messageSkus, token);

        // exceptions aren't caught and logged here; Wolverine logs handler failures, with the
        // exception, at Error level
        if (validSkus.Count != messageSkus.Count)
        {
            var invalidSkus = messageSkus.Except(validSkus.Select(s => s.Code));
            throw new MissingDependencyException(
                $"The following skus were not found: {string.Join(", ", invalidSkus)}");
        }

        var receipts = message.Pallets.Select(l =>
                SkuReceipt.Create(
                    EntityId.Create(),
                    validSkus.Single(s => s.Code == l.SkuCode).Id,
                    l.Quantity,
                    new AppDateTime(message.ReceivedOn)))
            .ToList();

        // permanent (poison) failure; retrying the same data can't succeed, so dead-letter it.
        var receiptErrors = receipts
            .Where(r => !r.Succeeded)
            .SelectMany(r => r.Errors)
            .Select(e => e.Description)
            .ToList();

        if (receiptErrors.Count > 0)
        {
            throw Poison(message, logger, receiptErrors);
        }

        foreach (var receipt in receipts)
        {
            // to prevent null ref warnings
            ArgumentNullException.ThrowIfNull(receipt.Value);

            inventoryRepository.Add(receipt.Value);

            logger.LogInformation(
                "Added new sku receipt for owner {OwnerId}, sku id {SkuId}",
                message.OwnerId, receipt.Value.SkuId);
        }
    }

    private static UnrecoverableMessageException Poison(
        AllocatablePalletsIntegrationEvent message,
        ILogger logger,
        IEnumerable<string> errors)
    {
        var reason = string.Join("; ", errors);

        logger.LogError(
            "Invalid AllocatablePallets for owner {OwnerId}, receipt id {ReceiptId}: {Reason}",
            message.OwnerId, message.ReceiptId, reason);

        return new UnrecoverableMessageException(
            $"AllocatablePallets for owner {message.OwnerId}, receipt id {message.ReceiptId} is invalid: {reason}");
    }
}