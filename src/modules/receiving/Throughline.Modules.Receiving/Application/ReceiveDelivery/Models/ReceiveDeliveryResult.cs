namespace Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;

internal sealed record ReceiveDeliveryResult
{
    public ReceiveDeliveryResult(
        Guid receiptId, string receiptNumber, IEnumerable<ReceivedPallet> pallets)
    {
        ReceiptId = receiptId;
        ReceiptNumber = receiptNumber;
        Pallets = pallets.ToList().AsReadOnly();
    }

    public Guid ReceiptId { get; }
    public string ReceiptNumber { get; }
    public IReadOnlyCollection<ReceivedPallet> Pallets { get; }
}