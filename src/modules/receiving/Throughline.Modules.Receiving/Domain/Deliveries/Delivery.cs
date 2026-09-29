using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Receiving.Domain.Inventory;
using Throughline.Modules.Receiving.Domain.Shipments;

namespace Throughline.Modules.Receiving.Domain.Deliveries;

internal sealed class Delivery : Entity<Guid>
{
    public Delivery(
        Guid id,
        int ownerId,
        Guid operatorId,
        AppDateTime receivedOn,
        Shipment shipment,
        IEnumerable<ReceivedPallet> receivedPallets,
        IEnumerable<InvalidPallet> invalidPallets) : base(id)
    {
        OwnerId = ownerId;
        OperatorId = operatorId;
        ReceivedOn = receivedOn;
        Shipment = shipment;
        ReceivedPallets = receivedPallets.ToList().AsReadOnly();
        InvalidPallets = invalidPallets.ToList().AsReadOnly();
    }

    public int OwnerId { get; }
    public Shipment Shipment { get; }
    public IReadOnlyCollection<ReceivedPallet> ReceivedPallets { get; }
    public IReadOnlyCollection<InvalidPallet> InvalidPallets { get; }

    /// <summary>
    ///     The id of the operator that received the delivery
    /// </summary>
    public Guid OperatorId { get; }

    public AppDateTime ReceivedOn { get; }

    public static Result<Delivery> Create(Guid id, int ownerId, Guid operatorId, AppDateTime receivedOn,
        Shipment shipment, IEnumerable<ReceivedPallet> receivedPallets, IEnumerable<InvalidPallet> invalidPallets)
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
        
        var duplicateReceived = received.GroupBy(grp => new {Lpn = grp.LicensePlateNumber.Trim().})

        if (receivedPallets.Any())
    }
}