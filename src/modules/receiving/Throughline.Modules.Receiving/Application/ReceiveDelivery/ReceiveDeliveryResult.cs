namespace Throughline.Modules.Receiving.Application.ReceiveDelivery;

internal sealed record ReceiveDeliveryResult(Guid ReceiptId, IEnumerable<ReceivedPallet> Pallets)
{
}