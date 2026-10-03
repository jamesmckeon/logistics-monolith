using Throughline.Modules.Receiving.Contracts.Events;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.Inventory;
using Throughline.Modules.Receiving.Domain.Locations;

namespace Throughline.Modules.Receiving.Domain.DeliveryReceipts;

internal interface IDeliveryReceiptRepository
{
    Task<DeliveryReceipt?> GetReceiptByIdAsync(int ownerId, Guid receiptId, CancellationToken token);

    Task<IReadOnlyCollection<ReceivedLpn>> GetReceivedLpnsAsync(int ownerId, IEnumerable<UpperCaseString> lpns,
        CancellationToken token);

    Task<IReadOnlyCollection<ReceivingLocation>> GetReceivingLocationsAsync(CancellationToken token);
    Task<IReadOnlyCollection<HoldReason>> GetHoldReasonsAsync(int ownerId, CancellationToken token);
    Task<ReceiptNumber?> GetLastReceiptNumberAsync(int ownerId, CancellationToken token);

    Task AddAsync(
        DeliveryReceipt receipt,
        AllocatablePalletsIntegrationEvent? @event,
        CancellationToken token);
}