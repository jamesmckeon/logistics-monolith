using Throughline.Modules.Receiving.Contracts.Events;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.Inventory;
using Throughline.Modules.Receiving.Domain.Locations;

namespace Throughline.Modules.Receiving.Domain.DeliveryReceipts;

internal interface IDeliveryReceiptRepository
{
    Task<DeliveryReceipt?> GetReceiptByIdAsync(int ownerId, Guid receiptId, CancellationToken token);

    Task<IReadOnlyCollection<UpperCaseString>> GetReceivedLpnsAsync(int ownerId, IEnumerable<UpperCaseString> lpns,
        CancellationToken token);

    Task<IReadOnlyCollection<ReceivingLocation>> GetReceivingLocationsAsync(CancellationToken token);
    Task<IReadOnlyCollection<HoldReason>> GetHoldReasonsAsync(int ownerId, CancellationToken token);
    /// <summary>
    ///     Issues the owner's next receipt number. The number is consumed when issued, committed on its own before
    ///     any receipt is saved: if the receipt then isn't saved, the number is never issued again and the owner's
    ///     sequence has a gap. Call it once per attempt, after every check that can reject the request.
    /// </summary>
    Task<ReceiptNumber> NextReceiptNumberAsync(int ownerId, CancellationToken token);

    Task AddAsync(
        DeliveryReceipt receipt,
        AllocatablePalletsIntegrationEvent? @event,
        CancellationToken token);
}