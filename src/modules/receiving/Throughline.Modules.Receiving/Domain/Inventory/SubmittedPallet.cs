namespace Throughline.Modules.Receiving.Domain.Inventory;

internal sealed record SubmittedPallet(
    string Lpn,
    string Sku,
    int Quantity,
    string LocationId,
    string? LotNumber,
    DateTime? Expires,
    string? HoldReasonCode)
{
}