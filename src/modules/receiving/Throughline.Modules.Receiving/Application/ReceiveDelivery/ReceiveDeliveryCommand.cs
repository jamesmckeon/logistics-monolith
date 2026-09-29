using Throughline.Common.Results;

namespace Throughline.Modules.Receiving.Application.ReceiveDelivery;

internal sealed record ReceiveDeliveryCommand(
    int OwnerId,
    Guid OperatorId,
    string DeliveryReference,
    string BillOfLading,
    string CarrierScac,
    string TrailerNumber,
    string ContainerNumber,
    string ShipperName,
    IEnumerable<ReceiveDeliveryCommandItem> Items)
{
    public Result ValidateCommand(ReceiveDeliveryCommand command) => throw new NotImplementedException();
}