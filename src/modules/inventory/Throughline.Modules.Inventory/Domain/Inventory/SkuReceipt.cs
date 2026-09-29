using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Common;

namespace Throughline.Modules.Inventory.Domain.Inventory;

internal sealed class SkuReceipt : Entity<EntityId>
{
    private SkuReceipt(
        EntityId id,
        EntityId skuId,
        int quantityReceived,
        int quantityAllocated,
        AppDateTime receivedOn,
        int quantityAvailable)
        : base(id)
    {
        SkuId = skuId;
        QuantityReceived = quantityReceived;
        QuantityAllocated = quantityAllocated;
        ReceivedOn = receivedOn;
        QuantityAvailable = quantityAvailable;
    }

    public EntityId SkuId { get; }
    public int QuantityReceived { get; }
    public int QuantityAllocated { get; private set; }
    public AppDateTime ReceivedOn { get; }
    public AppDateTime? LastUpdated { get; private set; }
    public int QuantityAvailable { get; private set; }

    public static Result<SkuReceipt> Create(
        EntityId id, EntityId skuId, int quantityReceived, AppDateTime receivedOn)
    {
        if (quantityReceived <= 0)
            return Result<SkuReceipt>.Validation("quantityReceived must be greater than zero");

        if (receivedOn > AppDateTime.Now)
            return Result<SkuReceipt>.Validation("receivedOn must be in the past");

        return new SkuReceipt(id, skuId, quantityReceived, 0, receivedOn, quantityReceived);
    }

    public void Allocate(int quantity, AppDateTime allocatedOn)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 0);

        if (quantity == 0)
            return;

        if (quantity > QuantityAvailable)
            throw new ArgumentOutOfRangeException(nameof(quantity), "quantity exceeds receipt's available quantity");

        if (allocatedOn > AppDateTime.Now)
            throw new ArgumentException("allocatedOn cannot be in the future", nameof(allocatedOn));

        QuantityAllocated += quantity;
        QuantityAvailable -= quantity;
        LastUpdated = allocatedOn;
    }
}