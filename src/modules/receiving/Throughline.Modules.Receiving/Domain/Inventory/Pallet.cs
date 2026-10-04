using System.Diagnostics.CodeAnalysis;
using Throughline.Common.Models;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.Locations;
using Throughline.Modules.Receiving.Domain.Skus;

namespace Throughline.Modules.Receiving.Domain.Inventory;

internal sealed class Pallet : Entity<UniqueId>
{
    [SetsRequiredMembers]
    public Pallet(
        UniqueId id,
        UpperCaseString licensePlateNumber,
        OwnerSku ownerSku,
        int quantity,
        ReceivingLocation location,
        HoldReason? holdReason,
        AppDateTime? expiresOn,
        string? lotNumber) : this(id, licensePlateNumber, quantity, expiresOn, lotNumber)
    {
        if (holdReason != null && !location.IsHoldLocation)
        {
            throw new InvalidOperationException("A held pallet must be received into a holding location");
        }

        if (ownerSku.IsExpirationTracked && expiresOn is null)
        {
            throw new InvalidOperationException($"sku #{ownerSku.SkuCode.Value} requires an expiration date");
        }

        if (ownerSku.IsLotTracked && string.IsNullOrWhiteSpace(lotNumber))
        {
            throw new InvalidOperationException($"sku #{ownerSku.SkuCode.Value} requires a lot #");
        }

        OwnerSku = ownerSku;
        Location = location;
        HoldReason = holdReason;
    }

    // EF materialization constructor; EF can't pass complex values (OwnerSku) or navigations (Location,
    // HoldReason) to a constructor, so it sets them after construction
    private Pallet(
        UniqueId id,
        UpperCaseString licensePlateNumber,
        int quantity,
        AppDateTime? expiresOn,
        string? lotNumber) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        LicensePlateNumber = licensePlateNumber;
        Quantity = quantity;
        ExpiresOn = expiresOn;
        LotNumber = lotNumber;
    }

    public UpperCaseString LicensePlateNumber { get; }
    public required OwnerSku OwnerSku { get; init; }
    public int Quantity { get; }
    public required ReceivingLocation Location { get; init; }
    public HoldReason? HoldReason { get; }
    public AppDateTime? ExpiresOn { get; }
    public string? LotNumber { get; }

    [MemberNotNullWhen(true, nameof(HoldReason))]
    public bool IsHeld => HoldReason is not null;

    /// <summary>
    ///     Returns true if pallet contents are valid for order allocation; this does not reflect
    ///     whether contents have yet been allocated or not
    /// </summary>
    public bool IsAllocatable => !IsHeld;
}