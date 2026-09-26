using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Common;

namespace Throughline.Modules.Inventory.Domain.Inventory;

internal sealed class SkuReceipt : Entity<EntityId>
{
    private readonly List<ReceiptAllocation> _allocations;

    // EF materialization constructor — binds mapped scalars and the _allocations backing field.
    private SkuReceipt(EntityId id, EntityId skuId, int quantityReceived, AppDateTime receivedOn)
        : base(id)
    {
        SkuId = skuId;
        QuantityReceived = quantityReceived;
        ReceivedOn = receivedOn;
        _allocations = [];
    }

    public EntityId SkuId { get; }
    public int QuantityReceived { get; }
    public AppDateTime ReceivedOn { get; }
    public IReadOnlyCollection<ReceiptAllocation> Allocations => _allocations.AsReadOnly();

    public int QuantityAvailable => QuantityReceived - _allocations.Sum(s => s.QuantityAllocated);

    public int AllocateToOrder(OrderAllocation order, int quantity, AppDateTime allocatedOn)
    {
        return AllocateToOrder(order.Id, quantity, allocatedOn);
    }

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

    public static Result<SkuReceipt> Create(
        EntityId id, EntityId skuId, int quantityReceived, AppDateTime receivedOn)
    {
        if (quantityReceived <= 0)
            return Result<SkuReceipt>.Validation("quantityReceived must be greater than zero");

        if (receivedOn > AppDateTime.Now)
            return Result<SkuReceipt>.Validation("receivedOn must be in the past");

        return new SkuReceipt(id, skuId, quantityReceived, receivedOn);
    }
}