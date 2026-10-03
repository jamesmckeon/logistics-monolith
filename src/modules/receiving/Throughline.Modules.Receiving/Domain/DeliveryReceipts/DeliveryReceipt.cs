using Throughline.Common.Models;
using Throughline.Modules.Receiving.Common.Exceptions;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.Inventory;
using Throughline.Modules.Receiving.Domain.Shipments;

namespace Throughline.Modules.Receiving.Domain.DeliveryReceipts;

internal sealed class DeliveryReceipt : Entity<Guid>
{
    public DeliveryReceipt(
        Guid receiptId,
        ReceiptNumber receiptNumber,
        int ownerId,
        Guid operatorId,
        AppDateTime receivedOn,
        Shipment shipment,
        IEnumerable<Pallet> receivedPallets,
        IEnumerable<InvalidPallet> invalidPallets) : base(receiptId)
    {
        FutureDateException.ThrowIfPastNow(receivedOn);

        var received = receivedPallets.ToArray();
        var invalid = invalidPallets.ToArray();

        if (!received.Any() && !invalid.Any())
        {
            throw new InvalidOperationException(
                "Either receivedPallets or invalidPallets must contain at least one item");
        }

        var duplicateReceived = received.GroupBy(grp => grp.LicensePlateNumber)
            .Select(s => new { Count = s.Count() })
            .Where(w => w.Count > 1)
            .ToList();

        if (duplicateReceived.Any())
        {
            throw new ArgumentException("receivedPallets must contain distinct LPNs", nameof(receivedPallets));
        }

        var duplicateInvalid = invalid.GroupBy(grp => grp.LicensePlateNumber)
            .Select(s => new { Count = s.Count() })
            .Where(w => w.Count > 1)
            .ToList();

        if (duplicateInvalid.Any())
        {
            throw new ArgumentException("invalidPallets must contain distinct LPNs", nameof(invalidPallets));
        }

        ReceiptNumber = receiptNumber;
        OwnerId = ownerId;
        OperatorId = operatorId;
        ReceivedOn = receivedOn;
        Shipment = shipment;
        Pallets = received.AsReadOnly();
        InvalidPallets = invalid.AsReadOnly();
    }

    public int OwnerId { get; }
    public ReceiptNumber ReceiptNumber { get; }
    public Shipment Shipment { get; }

    /// <summary>
    ///     The valid, not held pallets that were received into inventory as allocatable
    /// </summary>
    public IReadOnlyCollection<Pallet> Pallets { get; }

    public IReadOnlyCollection<InvalidPallet> InvalidPallets { get; }

    /// <summary>
    ///     The id of the operator that received the delivery
    /// </summary>
    public Guid OperatorId { get; }

    public AppDateTime ReceivedOn { get; }
}