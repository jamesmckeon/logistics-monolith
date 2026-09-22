using Throughline.Modules.Inventory.Domain.Common;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal sealed class TotalAllocationSpec : ISpecification<OrderAllocation>
{
    public bool IsSatisfiedBy(OrderAllocation obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return obj.AllocationStatus == AllocationStatus.Allocated;
    }
}