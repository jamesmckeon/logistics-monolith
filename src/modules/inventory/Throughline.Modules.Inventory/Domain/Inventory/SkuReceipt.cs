using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Common;

namespace Throughline.Modules.Inventory.Domain.Inventory;

internal sealed class SkuReceipt : Entity<EntityId>
{
    // EF materialization constructor — binds mapped scalars and the _allocations backing field.
    private SkuReceipt(
        EntityId id, EntityId skuId, int quantityReceived, int quantityAllocated, AppDateTime receivedOn)
        : base(id)
    {
        SkuId = skuId;
        QuantityReceived = quantityReceived;
        QuantityAllocated = quantityAllocated;
        ReceivedOn = receivedOn;
    }

    public EntityId SkuId { get; }
    public int QuantityReceived { get; }
    public int QuantityAllocated { get; }
    public AppDateTime ReceivedOn { get; }

    public int QuantityAvailable => QuantityReceived - QuantityAllocated;

    public static Result<SkuReceipt> Create(
        EntityId id, EntityId skuId, int quantityReceived, AppDateTime receivedOn)
    {
        if (quantityReceived <= 0)
            return Result<SkuReceipt>.Validation("quantityReceived must be greater than zero");

        if (receivedOn > AppDateTime.Now)
            return Result<SkuReceipt>.Validation("receivedOn must be in the past");

        return new SkuReceipt(id, skuId, quantityReceived, 0, receivedOn);
    }
}