using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Inventory;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal sealed class OrderLineAllocation : Entity<EntityId>
{
    private readonly List<ReceiptAllocation> _allocations;

    private OrderLineAllocation(
        EntityId id,
        EntityId skuId,
        int quantityRequested,
        int quantityAllocated,
        int quantityShort) : base(id)
    {
        ArgumentNullException.ThrowIfNull(skuId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantityRequested);
        ArgumentOutOfRangeException.ThrowIfNegative(quantityAllocated);
        ArgumentOutOfRangeException.ThrowIfNegative(quantityShort);

        SkuId = skuId;
        QuantityRequested = quantityRequested;
        QuantityAllocated = quantityAllocated;
        QuantityShort = quantityShort;

        _allocations = new();
    }

    public EntityId SkuId { get; }
    public int QuantityRequested { get; }
    public int QuantityAllocated { get; private set; }
    public int QuantityShort { get; private set; }
    public IReadOnlyCollection<ReceiptAllocation> ReceiptAllocations => _allocations;
    public AppDateTime? LastUpdated { get; private set; }

    public AllocationStatuses AllocationStatus
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

    public bool IsAllocatable => AllocationStatus != AllocationStatuses.Allocated;

    public static Result<OrderLineAllocation> Create(EntityId orderLineId, EntityId skuId, int quantityRequested)
    {
        ArgumentNullException.ThrowIfNull(orderLineId);
        ArgumentNullException.ThrowIfNull(skuId);

        if (quantityRequested <= 0)
            return Result<OrderLineAllocation>.Validation("quantityRequested must be greater than zero");

        return new OrderLineAllocation(orderLineId, skuId, quantityRequested, 0, quantityRequested);
    }

    public void AllocateReceipt(SkuReceipt receipt, AppDateTime allocatedOn)
    {
        if (receipt.SkuId != SkuId)
            throw new ArgumentException("Receipt sku must be the same as the line's sku",
                nameof(receipt));

        if (allocatedOn > AppDateTime.Now)
            throw new ArgumentException("allocatedOn must be in the past", nameof(allocatedOn));

        if (AllocationStatus == AllocationStatuses.Allocated)
            throw new InvalidOperationException("Cannot add a receipt to a fully allocated line");

        if (receipt.QuantityAvailable == 0)
            throw new InvalidOperationException("Cannot allocate a fully allocated receipt");

        var allocation = new ReceiptAllocation(
            receipt.Id,
            Math.Min(QuantityShort, receipt.QuantityAvailable),
            allocatedOn);
        _allocations.Add(allocation);

        QuantityAllocated += allocation.QuantityAllocated;
        QuantityShort = QuantityRequested - QuantityAllocated;
        LastUpdated = allocatedOn;

        receipt.Allocate(allocation.QuantityAllocated, allocatedOn);
    }

    public bool CanAllocateReceipt(SkuReceipt receipt)
    {
        if (receipt.SkuId != SkuId)
            return false;

        if (AllocationStatus == AllocationStatuses.Allocated)
            return false;

        if (receipt.QuantityAvailable == 0)
            return false;

        return true;
    }
}