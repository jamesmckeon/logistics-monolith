using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.Inventory;
using Throughline.Modules.Receiving.Domain.Shipments;

namespace Throughline.Modules.Receiving.Domain.DeliveryReceipts;

internal sealed class DeliveryReceipt : Entity<Guid>
{
    public DeliveryReceipt(
        Guid id,
        ReceiptNumber receiptNumber,
        int ownerId,
        Guid operatorId,
        AppDateTime receivedOn,
        Shipment shipment,
        IEnumerable<Pallet> receivedPallets,
        IEnumerable<InvalidPallet> invalidPallets) : base(id)
    {
        ReceiptNumber = receiptNumber;
        OwnerId = ownerId;
        OperatorId = operatorId;
        ReceivedOn = receivedOn;
        Shipment = shipment;
        ReceivedPallets = receivedPallets.ToList().AsReadOnly();
        InvalidPallets = invalidPallets.ToList().AsReadOnly();
    }

    public int OwnerId { get; }
    public ReceiptNumber ReceiptNumber { get; }
    public Shipment Shipment { get; }
    public IReadOnlyCollection<Pallet> ReceivedPallets { get; }
    public IReadOnlyCollection<InvalidPallet> InvalidPallets { get; }

    /// <summary>
    ///     The id of the operator that received the delivery
    /// </summary>
    public Guid OperatorId { get; }

    public AppDateTime ReceivedOn { get; }

    public static Result<DeliveryReceipt> Create(Guid id, ReceiptNumber receiptNumber, int ownerId, Guid operatorId,
        AppDateTime receivedOn,
        Shipment shipment, IEnumerable<Pallet> receivedPallets, IEnumerable<InvalidPallet> invalidPallets)
    {
        var errors = new List<FieldError>();

        if (receivedOn > AppDateTime.Now)
        {
            errors.Add(new("receivedOn must be in the past"));
        }

        var received = receivedPallets.ToArray();
        var invalid = invalidPallets.ToArray();

        if (!received.Any() && !invalid.Any())
        {
            errors.Add(new("Either receivedPallets or invalidPallets must contain at least one item"));
        }

        var duplicateReceived = received.GroupBy(grp => grp.LicensePlateNumber)
            .Select(s => new { Count = s.Count() })
            .Where(w => w.Count > 1)
            .ToList();

        if (duplicateReceived.Any())
        {
            errors.Add(new FieldError("receivedPallets must contain distinct LPNs"));
        }

        var duplicateInvalid = invalid.GroupBy(grp => grp.LicensePlateNumber)
            .Select(s => new { Count = s.Count() })
            .Where(w => w.Count > 1)
            .ToList();

        if (duplicateInvalid.Any())
        {
            errors.Add(new FieldError("invalidPallets must contain distinct LPNs"));
        }

        if (errors.Any())
        {
            return Result<DeliveryReceipt>.Validation(errors);
        }

        return new DeliveryReceipt(
            id, receiptNumber, ownerId, operatorId, receivedOn, shipment, received, invalid);
    }
}