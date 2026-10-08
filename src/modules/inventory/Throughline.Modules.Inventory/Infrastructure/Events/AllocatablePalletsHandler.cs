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
        ILogger<AllocatablePalletsHandler> logger,
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
        var invalidSkus = messageSkus.Where(s => validSkus.All(vs => vs.Code != s))
            .ToList();

        if (invalidSkus.Any())
        {
            throw new MissingDependencyException(
                $"The following skus were not found: {string.Join(", ", invalidSkus)}");
        }

        var validPallets = message.Pallets.Where(p => validSkus.Any(vs => vs.Code == p.SkuCode));

        var lpnErrors = new List<LpnError>();

        // for each pallet, create a sku receipt
        foreach (var pallet in validPallets)
        {
            var sku = validSkus.First(f => f.Code == pallet.SkuCode);
            var receiptResult = SkuReceipt.Create(
                EntityId.Create(), sku.Id, pallet.Quantity, new AppDateTime(message.ReceivedOn));

            if (receiptResult.Succeeded)
            {
                inventoryRepository.Add(receiptResult.Value);
            }
            else
            {
                lpnErrors.Add(new(pallet.Lpn, receiptResult.Errors.Select(e => e.Description)));
            }
        }

        // All or nothing: throwing means [Transactional] never saves the receipts added above, so the dead-lettered
        // message has nothing partially applied and can be replayed once the publisher is fixed
        if (lpnErrors.Any())
        {
            throw Poison(message, logger, lpnErrors);
        }
    }

    private static UnrecoverableMessageException Poison(
        AllocatablePalletsIntegrationEvent message,
        ILogger logger,
        IEnumerable<LpnError> errors)
    {
        var reason = string.Join("; ", errors.Select(e => e.ToExceptionMessage()));

        logger.LogError(
            "Invalid AllocatablePallets for owner {OwnerId}, receipt id {ReceiptId}: {Reason}",
            message.OwnerId, message.ReceiptId, reason);

        return new UnrecoverableMessageException(
            $"The following pallet LPNs passed by an AllocatablePalletsIntegrationEvent for owner {message.OwnerId}, " +
            $"receipt id {message.ReceiptId} are invalid: {reason}");
    }

    private sealed record LpnError(string Lpn, IEnumerable<string> Errors)
    {
        public string ToExceptionMessage() => $"LPN {Lpn}: {string.Join("; ", Errors)}";
    }
}