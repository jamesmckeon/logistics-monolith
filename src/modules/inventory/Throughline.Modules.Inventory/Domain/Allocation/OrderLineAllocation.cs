using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Common;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal sealed class OrderLineAllocation : Entity<EntityId>
{
    private OrderLineAllocation(
        EntityId id,
        EntityId skuId,
        int quantityRequested,
        int quantityAllocated) : base(id)
    {
        ArgumentNullException.ThrowIfNull(skuId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantityRequested);

        SkuId = skuId;
        QuantityRequested = quantityRequested;
        QuantityAllocated = quantityAllocated;
    }

    public EntityId SkuId { get; }
    public int QuantityRequested { get; }
    public int QuantityAllocated { get; private set; }
    public int QuantityShort => QuantityRequested - QuantityAllocated;
    public AppDateTime? LastUpdated { get; private set; }

    public AllocationStatuses AllocationStatuses
    {
        get
        {
            if (QuantityAllocated == 0)
                return AllocationStatuses.Confirmed;
            if (QuantityAllocated < QuantityRequested)
                return AllocationStatuses.PartiallyAllocated;

            return AllocationStatuses.Allocated;
        }
    }

    public bool IsAllocatable => AllocationStatuses != AllocationStatuses.Allocated;

    public static Result<OrderLineAllocation> Create(EntityId orderLineId, EntityId skuId, int quantityRequested)
    {
        ArgumentNullException.ThrowIfNull(orderLineId);
        ArgumentNullException.ThrowIfNull(skuId);

        if (quantityRequested <= 0)
            return Result<OrderLineAllocation>.Validation("quantityRequested must be greater than zero");

        return new OrderLineAllocation(orderLineId, skuId, quantityRequested, 0);
    }

    public void IncreaseQuantityAllocated(int quantity, AppDateTime updatedOn)
    {
        ArgumentNullException.ThrowIfNull(updatedOn);

        if (!IsAllocatable)
            throw new InvalidOperationException("line isn't allocatable");

        if (quantity == 0)
            return;

        if (QuantityAllocated + quantity > QuantityRequested)
            throw new InvalidOperationException(
                $"Increasing the allocated quantity by {quantity} " +
                "would exceed the requested quantity for this order line");

        QuantityAllocated += quantity;
        LastUpdated = updatedOn;
    }
}