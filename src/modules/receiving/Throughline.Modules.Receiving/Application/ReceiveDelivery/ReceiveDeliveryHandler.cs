using Throughline.Modules.Receiving.Domain.Deliveries;

namespace Throughline.Modules.Receiving.Application.ReceiveDelivery;

internal sealed class ReceiveDeliveryHandler
{
    private readonly IDeliveryRepository _deliveryRepository;

    public ReceiveDeliveryHandler(IDeliveryRepository deliveryRepository)
    {
        _deliveryRepository = deliveryRepository;
    }


    public Task<ReceiveDeliveryResult> ReceiveDeliveryAsync(ReceiveDeliveryCommand command, CancellationToken token) =>
        throw new NotImplementedException();
}