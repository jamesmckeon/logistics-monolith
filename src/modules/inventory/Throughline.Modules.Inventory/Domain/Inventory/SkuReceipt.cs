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
        int quantityAvailable,
        Guid? createdBy)
        : base(id)
    {
        SkuId = skuId;
        QuantityReceived = quantityReceived;
        QuantityAllocated = quantityAllocated;
        ReceivedOn = receivedOn;
        QuantityAvailable = quantityAvailable;
        CreatedBy = createdBy;
    }

    public EntityId SkuId { get; }
    public int QuantityReceived { get; }
    public int QuantityAllocated { get; private set; }
    public AppDateTime ReceivedOn { get; }
    public AppDateTime? LastUpdated { get; private set; }
    public int QuantityAvailable { get; private set; }

    /// <summary>
    ///     The Entra object ID (<c>oid</c>) of the caller who received the delivery. Null for receipts created
    ///     before it was recorded, and for messages stored before events carried it.
    /// </summary>
    public Guid? CreatedBy { get; }

    /// <summary>
    ///     The Entra object ID (<c>oid</c>) of the caller who last allocated from the receipt; set with
    ///     <see cref="LastUpdated" />
    /// </summary>
    public Guid? UpdatedBy { get; private set; }

    /// <param name="createdBy">The Entra object ID (<c>oid</c>) of the caller who received the delivery</param>
    public static Result<SkuReceipt> Create(
        EntityId id, EntityId skuId, int quantityReceived, AppDateTime receivedOn, Guid? createdBy)
    {
        if (quantityReceived <= 0)
            return Result<SkuReceipt>.Validation("quantityReceived must be greater than zero");

        if (receivedOn > AppDateTime.Now)
            return Result<SkuReceipt>.Validation("receivedOn must be in the past");

        return new SkuReceipt(id, skuId, quantityReceived, 0, receivedOn, quantityReceived, createdBy);
    }

    /// <param name="allocatedBy">The Entra object ID (<c>oid</c>) of the caller allocating the order</param>
    public void Allocate(int quantity, AppDateTime allocatedOn, Guid allocatedBy)
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
        UpdatedBy = allocatedBy;
    }
}