using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.Shipments;

namespace Throughline.Modules.Receiving.Domain.Inventory;

/// <summary>
///     A pallet that hasn't been received into inventory due to invalid state
/// </summary>
internal sealed class InvalidPallet : Entity<UniqueId>
{
    private InvalidPallet(
        UniqueId id,
        int ownerId,
        InvalidShipment shipment,
        UpperCaseString skuCode,
        UpperCaseString lpn,
        int quantity,
        IEnumerable<ReceivingExceptions> exceptions) : base(id)
    {
        OwnerId = ownerId;
        Shipment = shipment;
        SkuCode = skuCode;
        Quantity = quantity;
        LicensePlateNumber = lpn;
        Exceptions = exceptions.ToArray().AsReadOnly();
    }

    public int OwnerId { get; }
    public InvalidShipment Shipment { get; }
    public UpperCaseString SkuCode { get; }
    public UpperCaseString LicensePlateNumber { get; }
    public int Quantity { get; }
    public IReadOnlyCollection<ReceivingExceptions> Exceptions { get; }

    public static Result<InvalidPallet> Create(UniqueId id, int ownerId, InvalidShipment shipment,
        UpperCaseString skuCode,
        UpperCaseString lpn, int quantity, params ReceivingExceptions[] exceptions)
    {
        if (quantity <= 0)
        {
            return Result<InvalidPallet>.Validation(new FieldError("quantity must be greater than zero"));
        }

        if (exceptions.Length == 0)
        {
            return Result<InvalidPallet>.Validation(new FieldError("exceptions must contain at least one item"));
        }

        return new InvalidPallet(id, ownerId, shipment, skuCode, lpn, quantity, exceptions.Distinct());
    }
}