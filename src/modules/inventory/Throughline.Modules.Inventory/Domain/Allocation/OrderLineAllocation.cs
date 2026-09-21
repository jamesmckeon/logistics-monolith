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
    public int QuantityUnallocated => QuantityRequested - QuantityAllocated;
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

        if (QuantityUnallocated + quantity > QuantityRequested)
            throw new InvalidOperationException(
                $"Increasing the allocated quantity by {quantity} " +
                "would exceed the requested quantity for this order line");

        QuantityAllocated += quantity;
        LastUpdated = updatedOn;
    }

    public void SetAllocated(AppDateTime updatedOn)
    {
        ArgumentNullException.ThrowIfNull(updatedOn);

        if (QuantityAllocated != QuantityRequested)
            throw new InvalidOperationException(
                "AllocationStatus cannot be changed to Allocated if QuantityRequested <> QuantityAllocated");

        if (AllocationStatus != AllocationStatus.Allocated)
        {
            AllocationStatus = AllocationStatus.Allocated;
            LastUpdated = updatedOn;
        }
    }

    public void SetPartiallyAllocated(AppDateTime updatedOn)
    {
        ArgumentNullException.ThrowIfNull(updatedOn);

        if (AllocationStatus == AllocationStatus.Allocated)
            throw new InvalidOperationException(
                "A fully allocated order line cannot be reverted to partially allocated");

        if (AllocationStatus != AllocationStatus.PartiallyAllocated)
        {
            AllocationStatus = AllocationStatus.PartiallyAllocated;
            LastUpdated = updatedOn;
        }
    }
}