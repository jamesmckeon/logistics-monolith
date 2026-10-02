using Throughline.Modules.Receiving.Domain.Common;

namespace Throughline.Modules.Receiving.Domain.DeliveryReceipts;

internal interface IDeliveryReceiptRepository
{
    Task<DeliveryReceipt?> GetReceiptByIdAsync(int ownerId, Guid deliveryId, CancellationToken token);

    Task<IReadOnlyCollection<ReceivedLpn>> GetReceivedLpnsAsync(int ownerId, IEnumerable<UpperCaseString> lpns,
        CancellationToken token);
}