using Throughline.Modules.Receiving.Domain.Common;

namespace Throughline.Modules.Receiving.Domain.Deliveries;

internal interface IDeliveryRepository
{
    Task<Delivery?> GetDeliveryByIdAsync(int ownerId, Guid deliveryId, CancellationToken token);

    Task<IReadOnlyCollection<ReceivedLpn>> GetReceivedLpnsAsync(int ownerId, IEnumerable<UpperCaseString> lpns,
        CancellationToken token);
}