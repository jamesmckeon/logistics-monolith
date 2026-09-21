using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Skus;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal sealed class OrderLineAllocation : ValueObject
{
    private OrderLineAllocation(
        Sku sku, int quantityRequested, AllocationStatus allocationStatus, int quantityAllocated)
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantityRequested);

        Sku = sku;
        QuantityRequested = quantityRequested;
        AllocationStatus = allocationStatus;
    }

    public Sku Sku { get; }
    public int QuantityRequested { get; }
    public int QuantityAllocated { get; private set; }
    public AppDateTime? LastAllocated { get; private set; }
    public AllocationStatus AllocationStatus { get; private set; }
    public bool IsAllocatable => AllocationStatus != AllocationStatus.Allocated;

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Sku;
        yield return QuantityRequested;
    }

    public static Result<OrderLineAllocation> Create(Sku sku, int quantityRequested)
    {
        ArgumentNullException.ThrowIfNull(sku);

        if (quantityRequested <= 0)
            return Result<OrderLineAllocation>.Validation("quantityRequested must be greater than zero");

        return new OrderLineAllocation(sku, quantityRequested, AllocationStatus.Confirmed, 0);
    }

    public void UpdateAllocatedQuantity(int allocatedQuantity, AppDateTime updatedOn)
    {
        ArgumentNullException.ThrowIfNull(updatedOn);

        if (!IsAllocatable)
            throw new InvalidOperationException("line isn't allocatable");

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(allocatedQuantity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(allocatedQuantity, QuantityRequested);

        QuantityAllocated = allocatedQuantity;
        AllocationStatus = QuantityAllocated == QuantityRequested
            ? AllocationStatus.Allocated
            : AllocationStatus.PartiallyAllocated;

        LastAllocated = updatedOn;
    }
}