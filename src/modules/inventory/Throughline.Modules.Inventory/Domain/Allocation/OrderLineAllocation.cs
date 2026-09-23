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
        AllocationStatus allocationStatus,
        int quantityAllocated) : base(id)
    {
        ArgumentNullException.ThrowIfNull(skuId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantityRequested);

        SkuId = skuId;
        QuantityRequested = quantityRequested;
        AllocationStatus = allocationStatus;
        QuantityAllocated = quantityAllocated;
    }

    public EntityId SkuId { get; }
    public int QuantityRequested { get; }
    public int QuantityAllocated { get; private set; }
    public int QuantityShort => QuantityRequested - QuantityAllocated;
    public AppDateTime? LastUpdated { get; private set; }
    public AllocationStatus AllocationStatus { get; private set; }
    public bool IsAllocatable => AllocationStatus != AllocationStatus.Allocated;

    public static Result<OrderLineAllocation> Create(EntityId id, EntityId skuId, int quantityRequested)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(skuId);

        if (quantityRequested <= 0)
            return Result<OrderLineAllocation>.Validation("quantityRequested must be greater than zero");

        return new OrderLineAllocation(id, skuId, quantityRequested, AllocationStatus.Confirmed, 0);
    }

    public void IncreaseQuantityAllocated(int quantity, AppDateTime updatedOn)
    {
        ArgumentNullException.ThrowIfNull(updatedOn);

        if (!IsAllocatable)
            throw new InvalidOperationException("line isn't allocatable");

        if (quantity == 0)
            return;

        if (QuantityShort + quantity > QuantityRequested)
            throw new InvalidOperationException(
                $"Increasing the allocated quantity by {quantity} " +
                "would exceed the requested quantity for this order line");

        QuantityAllocated += quantity;
        LastUpdated = updatedOn;

        if (QuantityAllocated == QuantityRequested)
            AllocationStatus = AllocationStatus.Allocated;

        if (QuantityAllocated < QuantityRequested)
            AllocationStatus = AllocationStatus.PartiallyAllocated;

        // if requested quantity is increased, it can never be status "Confirmed"
        // which is the baseline/start status
    }
}