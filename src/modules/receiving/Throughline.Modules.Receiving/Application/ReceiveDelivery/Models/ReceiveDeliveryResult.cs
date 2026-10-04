using System.Text.Json.Serialization;

namespace Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;

internal sealed record ReceiveDeliveryResult
{
    public ReceiveDeliveryResult(
        Guid receiptId, string receiptNumber, IEnumerable<ReceivedPallet> pallets)
        : this(receiptId, receiptNumber, pallets.ToList().AsReadOnly())
    {
    }

    // Deserialization constructor for replaying a stored result; System.Text.Json requires each
    // parameter's type to match its property's type
    [JsonConstructor]
    private ReceiveDeliveryResult(
        Guid receiptId, string receiptNumber, IReadOnlyCollection<ReceivedPallet> pallets)
    {
        ReceiptId = receiptId;
        ReceiptNumber = receiptNumber;
        Pallets = pallets;
    }

    public Guid ReceiptId { get; }
    public string ReceiptNumber { get; }
    public IReadOnlyCollection<ReceivedPallet> Pallets { get; }
}
