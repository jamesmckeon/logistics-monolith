using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Owners;

namespace Throughline.Modules.Inventory.Domain.Skus;

internal sealed class OwnerSku : ValueObject
{
    public OwnerSku(Owner owner, SkuCode skuCode)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(skuCode);

        Owner = owner;
        SkuCode = skuCode;
    }

    public Owner Owner { get; }
    public SkuCode SkuCode { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Owner;
        yield return SkuCode;
    }
}