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
        int quantityAllocated) : base(id)
    {
        ArgumentNullException.ThrowIfNull(skuId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantityRequested);

        SkuId = skuId;
        QuantityRequested = quantityRequested;
        QuantityAllocated = quantityAllocated;
        _allocations = new();
    }

    public EntityId SkuId { get; }
    public int QuantityRequested { get; }
    public int QuantityAllocated { get; }
    public int QuantityShort => QuantityRequested - QuantityAllocated;
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

        return new OrderLineAllocation(orderLineId, skuId, quantityRequested, 0);
    }

    public void AllocateReceipt(SkuReceipt receipt, AppDateTime allocatedOn)
    {
        throw new NotImplementedException();
    }

    public bool CanAllocate(SkuReceipt receipt)
    {
        throw new NotImplementedException();
    }
}