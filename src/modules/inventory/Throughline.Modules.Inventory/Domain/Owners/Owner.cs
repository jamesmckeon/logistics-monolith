using Throughline.Common.Models;

namespace Throughline.Modules.Inventory.Domain.Owners;

internal sealed class Owner : ValueObject
{
    public Owner(int id, AllocationPolicies policy)
    {
        Id = id;
        AllocationPolicy = policy;
    }

    public int Id { get; }
    public AllocationPolicies AllocationPolicy { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Id;
        yield return AllocationPolicy;
    }
}