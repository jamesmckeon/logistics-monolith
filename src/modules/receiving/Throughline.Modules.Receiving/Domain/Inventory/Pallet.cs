using System.Diagnostics.CodeAnalysis;
using Throughline.Common.Models;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.Locations;
using Throughline.Modules.Receiving.Domain.Skus;

namespace Throughline.Modules.Receiving.Domain.Inventory;

internal sealed class Pallet : Entity<UniqueId>
{
    public Pallet(
        UniqueId id,
        UpperCaseString licensePlateNumber,
        OwnerSku ownerSku,
        int quantity,
        ReceivingLocation location,
        HoldReason? holdReason,
        AppDateTime? expiresOn,
        string? lotNumber) : base(id)
    {
        if (holdReason != null && location.IsHoldLocation)
        {
            throw new InvalidOperationException("A held pallet must be received into a holding location");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        if (ownerSku.IsExpirationTracked && expiresOn is null)
        {
            throw new InvalidOperationException($"sku #{ownerSku.SkuCode.Value} requires an expiration date");
        }

        if (ownerSku.IsLotTracked && string.IsNullOrWhiteSpace(lotNumber))
        {
            throw new InvalidOperationException($"sku #{ownerSku.SkuCode.Value} requires a lot #");
        }

        LicensePlateNumber = licensePlateNumber;
        OwnerSku = ownerSku;
        Quantity = quantity;
        Location = location;
        HoldReason = holdReason;
        ExpiresOn = expiresOn;
        LotNumber = lotNumber;
    }

    public UpperCaseString LicensePlateNumber { get; }
    public OwnerSku OwnerSku { get; }
    public int Quantity { get; }
    public ReceivingLocation Location { get; }
    public HoldReason? HoldReason { get; }
    public AppDateTime? ExpiresOn { get; }
    public string? LotNumber { get; }

    [MemberNotNullWhen(true, nameof(HoldReason))]
    public bool IsHeld => HoldReason is not null;
}