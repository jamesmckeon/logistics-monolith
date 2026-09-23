using Throughline.Modules.Inventory.Domain.Common;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal sealed class AllocationSpecContext
{
    private ISpecification<OrderAllocation> GetSpecification(AllocationPolicies policy)
    {
        switch (policy)
        {
            case AllocationPolicies.ShipComplete:
                return new TotalAllocationSpec();
            case AllocationPolicies.Partial:
                return new PartialAllocationSpec();
            default:
                throw new NotSupportedException();
        }
    }
}