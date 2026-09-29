using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Allocation;

namespace Throughline.Modules.Inventory.Domain.Owners;

internal sealed class Owner : ValueObject
{
    public Owner(int id, AllocationPolicies allocationPolicy)
    {
        Id = id;
        AllocationPolicy = allocationPolicy;
    }

    public int Id { get; }
    public AllocationPolicies AllocationPolicy { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Id;
        yield return AllocationPolicy;
    }
}