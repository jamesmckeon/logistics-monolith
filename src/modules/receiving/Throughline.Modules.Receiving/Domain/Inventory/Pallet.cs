using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.Locations;

namespace Throughline.Modules.Receiving.Domain.Inventory;

internal sealed class Pallet : Entity<Guid>
{
    private Pallet(
        Guid id,
        UpperCaseString licensePlateNumber,
        UpperCaseString skuCode,
        int quantity,
        ReceivingLocation location,
        HoldReasons? holdReason) : base(id)
    {
        LicensePlateNumber = licensePlateNumber;
        SkuCode = skuCode;
        Quantity = quantity;
        Location = location;
        HoldReason = holdReason;
    }

    public UpperCaseString LicensePlateNumber { get; }
    public UpperCaseString SkuCode { get; }
    public int Quantity { get; }
    public ReceivingLocation Location { get; }
    public HoldReasons? HoldReason { get; }

    public static Result<Pallet> Create(Guid id, UpperCaseString lpn, UpperCaseString skuCode,
        int quantity, ReceivingLocation location, HoldReasons? holdReason)
    {
        if (holdReason != null && location.IsHoldLocation())
        {
            return Result<Pallet>.Validation(
                new FieldError("A held pallet must be received into a holding location"));
        }

        if (quantity <= 0)
        {
            return Result<Pallet>.Validation(new FieldError("quantity must be greater than zero"));
        }

        return new Pallet(id, lpn, skuCode, quantity, location, holdReason);
    }
}