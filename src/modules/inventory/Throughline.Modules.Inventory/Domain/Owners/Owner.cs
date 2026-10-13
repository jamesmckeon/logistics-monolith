using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Allocation;

namespace Throughline.Modules.Inventory.Domain.Owners;

internal sealed class Owner : ValueObject
{
    public Owner(int id, string name, AllocationPolicies allocationPolicy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Name = name;
        AllocationPolicy = allocationPolicy;
    }

    public int Id { get; }
    public string Name { get; }
    public AllocationPolicies AllocationPolicy { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Id;
        yield return Name;
        yield return AllocationPolicy;
    }
}