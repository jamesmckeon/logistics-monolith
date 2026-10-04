using System.Diagnostics.CodeAnalysis;
using Throughline.Common.Models;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.Inventory;
using Throughline.Modules.Receiving.Domain.Shipments;

namespace Throughline.Modules.Receiving.Domain.DeliveryReceipts;

internal sealed class DeliveryReceipt : Entity<Guid>
{
    private readonly List<InvalidPallet> _invalidPallets;
    private readonly List<Pallet> _pallets;

    [SetsRequiredMembers]
    public DeliveryReceipt(
        Guid receiptId,
        ReceiptNumber receiptNumber,
        int ownerId,
        Guid operatorId,
        NonFutureDateTime receivedOn,
        Shipment shipment,
        IEnumerable<Pallet> receivedPallets,
        IEnumerable<InvalidPallet> invalidPallets) : this(receiptId, receiptNumber, ownerId, operatorId, receivedOn)
    {
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

        Shipment = shipment;
        _pallets.AddRange(received);
        _invalidPallets.AddRange(invalid);
    }

    // EF materialization constructor; EF can't pass complex values (Shipment) or navigations (the pallet
    // collections) to a constructor, so it sets Shipment and fills the collections after construction
    private DeliveryReceipt(
        Guid id,
        ReceiptNumber receiptNumber,
        int ownerId,
        Guid operatorId,
        NonFutureDateTime receivedOn) : base(id)
    {
        ReceiptNumber = receiptNumber;
        OwnerId = ownerId;
        OperatorId = operatorId;
        ReceivedOn = receivedOn;
        _pallets = [];
        _invalidPallets = [];
    }

    public int OwnerId { get; }
    public ReceiptNumber ReceiptNumber { get; }
    public required Shipment Shipment { get; init; }

    /// <summary>
    ///     The valid, not held pallets that were received into inventory as allocatable
    /// </summary>
    public IReadOnlyCollection<Pallet> Pallets => _pallets.AsReadOnly();

    public IReadOnlyCollection<InvalidPallet> InvalidPallets => _invalidPallets.AsReadOnly();

    /// <summary>
    ///     The id of the operator that received the delivery
    /// </summary>
    public Guid OperatorId { get; }

    public NonFutureDateTime ReceivedOn { get; }
}