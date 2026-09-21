using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Common;

namespace Throughline.Modules.Inventory.Domain.Inventory;

internal sealed class SkuReceipt : Entity<EntityId>
{
    private readonly List<ReceiptAllocation> _allocations;

/*
    private SkuReceipt(Sku sku, int quantityReceived, AppDateTime receivedOn)
        : base(Guid.CreateVersion7())
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantityReceived);
        ArgumentNullException.ThrowIfNull(receivedOn);

        // Reference the Sku aggregate by identity, not by object; owner is denormalized for
        // segregation (every aggregate carries its own OwnerId).
        SkuId = sku.Id;
        OwnerId = sku.OwnerId;
        QuantityReceived = quantityReceived;
        ReceivedOn = receivedOn;
        _allocations = [];
    }
*/
    // EF materialization constructor — binds mapped scalars and the _allocations backing field.
    private SkuReceipt(EntityId id, EntityId skuId, int ownerId, int quantityReceived, AppDateTime receivedOn)
        : base(id)
    {
        SkuId = skuId;
        OwnerId = ownerId;
        QuantityReceived = quantityReceived;
        ReceivedOn = receivedOn;
        _allocations = [];
    }

    public EntityId SkuId { get; }
    public int OwnerId { get; }
    public int QuantityReceived { get; }
    public AppDateTime ReceivedOn { get; }
    public IReadOnlyCollection<ReceiptAllocation> Allocations => _allocations.AsReadOnly();

    public int QuantityAvailable => QuantityReceived - _allocations.Sum(s => s.QuantityAllocated);

    public int AllocateToOrder(Guid orderid, int quantity, AppDateTime allocatedOn)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentNullException.ThrowIfNull(allocatedOn);

        if (QuantityAvailable >= quantity)
        {
            var allocationQuantity = Math.Min(QuantityAvailable, quantity);
            _allocations.Add(new(orderid, allocationQuantity, allocatedOn));
            return allocationQuantity;
        }

        return 0;
    }
}