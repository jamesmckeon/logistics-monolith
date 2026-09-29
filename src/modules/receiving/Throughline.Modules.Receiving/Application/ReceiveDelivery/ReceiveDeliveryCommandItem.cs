namespace Throughline.Modules.Receiving.Application.ReceiveDelivery;

internal sealed record ReceiveDeliveryCommandItem(
    string Lpn,
    string Sku,
    int Quantity,
    string LocationId,
    string? LotNumber,
    DateTime? Expires,
    string? HoldReasonCode)
{
}