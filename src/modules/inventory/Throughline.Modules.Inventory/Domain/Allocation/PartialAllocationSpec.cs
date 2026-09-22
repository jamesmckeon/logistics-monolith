using Throughline.Modules.Inventory.Domain.Common;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal sealed class PartialAllocationSpec : ISpecification<OrderAllocation>
{
    public bool IsSatisfiedBy(OrderAllocation obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return obj.AllocationStatus == AllocationStatus.PartiallyAllocated ||
               obj.AllocationStatus == AllocationStatus.Confirmed;
    }
}