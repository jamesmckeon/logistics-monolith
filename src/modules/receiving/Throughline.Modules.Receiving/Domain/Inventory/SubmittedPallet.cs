namespace Throughline.Modules.Receiving.Domain.Inventory;

internal sealed record SubmittedPallet(
    string Lpn,
    string Sku,
    int Quantity,
    string LocationId,
    string? LotNumber = null,
    DateTime? Expires = null,
    string? HoldReasonCode = null)
{
}