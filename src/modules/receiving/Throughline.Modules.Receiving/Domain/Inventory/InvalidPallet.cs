using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.Shipments;

namespace Throughline.Modules.Receiving.Domain.Inventory;

/// <summary>
///     A pallet that hasn't been received into inventory due to invalid state
/// </summary>
internal sealed class InvalidPallet : Entity<Guid>
{
    private InvalidPallet(
        Guid id, int ownerId, Shipment shipment, UpperCaseString skuCode, UpperCaseString lpn, int quantity) : base(id)
    {
        OwnerId = ownerId;
        Shipment = shipment;
        SkuCode = skuCode;
        Quantity = quantity;
        LicensePlateNumber = lpn;
    }

    public int OwnerId { get; }
    public Shipment Shipment { get; }
    public UpperCaseString SkuCode { get; }
    public UpperCaseString LicensePlateNumber { get; }
    public int Quantity { get; }

    public static Result<InvalidPallet> Create(Guid id, int ownerId, Shipment shipment, UpperCaseString skuCode,
        UpperCaseString lpn, int quantity) =>
        throw new NotImplementedException();
}