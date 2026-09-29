using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Domain.Locations;

namespace Throughline.Modules.Inventory.Domain.Receiving;

internal sealed class InventoryPallet : Entity<EntityId>
{
    private InventoryPallet(
        EntityId id,
        EntityId skuId,
        int quantityReceived,
        int quantityAllocated,
        AppDateTime receivedOn,
        int quantityAvailable,
        InventoryLocation location,
        Lpn lpn,
        string? lotNumber,
        AppDateTime? expirationDate,
        HoldCodes? holdCode)
        : base(id)
    {
        SkuId = skuId;
        QuantityReceived = quantityReceived;
        QuantityAllocated = quantityAllocated;
        ReceivedOn = receivedOn;
        QuantityAvailable = quantityAvailable;
        Location = location;
        Lpn = lpn;
        LotNumber = lotNumber;
        ExpirationDate = expirationDate;
        HoldCode = holdCode;
    }

    public EntityId SkuId { get; }
    public Lpn Lpn { get; private set; }
    public int QuantityReceived { get; }
    public int QuantityAllocated { get; private set; }
    public AppDateTime ReceivedOn { get; }
    public AppDateTime? LastUpdated { get; private set; }
    public int QuantityAvailable { get; private set; }
    public InventoryLocation Location { get; private set; }
    public string? LotNumber { get; }
    public AppDateTime? ExpirationDate { get; }
    public HoldCodes? HoldCode { get; }

    public static Result<InventoryPallet> Create(
        EntityId id, EntityId skuId, int quantityReceived, AppDateTime receivedOn,
        InventoryLocation location, Lpn lpn, string? lotNumber, AppDateTime? expirationDate,
        HoldCodes? holdCode)
    {
        if (quantityReceived <= 0)
        {
            return Result<InventoryPallet>.Validation("quantityReceived must be greater than zero");
        }

        if (receivedOn > AppDateTime.Now)
        {
            return Result<InventoryPallet>.Validation("receivedOn must be in the past");
        }

        if (location.LocationType != InventoryLocationTypes.Bulk)
        {
            return Result<InventoryPallet>.Validation("A pallet must be received into a bulk location");
        }

        return new InventoryPallet(id, skuId, quantityReceived, 0, receivedOn, quantityReceived,
            location, lpn, lotNumber, expirationDate, holdCode);
    }

    public void Allocate(int quantity, AppDateTime allocatedOn)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 0);

        if (quantity == 0)
        {
            return;
        }

        if (quantity > QuantityAvailable)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "quantity exceeds receipt's available quantity");
        }

        if (allocatedOn > AppDateTime.Now)
        {
            throw new ArgumentException("allocatedOn cannot be in the future", nameof(allocatedOn));
        }

        QuantityAllocated += quantity;
        QuantityAvailable -= quantity;
        LastUpdated = allocatedOn;
    }
}