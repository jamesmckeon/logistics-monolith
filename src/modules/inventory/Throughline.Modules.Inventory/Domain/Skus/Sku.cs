using Throughline.Modules.Inventory.Domain.Owners;

namespace Throughline.Modules.Inventory.Domain.Skus;

internal sealed record Sku(Owner Owner, string SkuCode)
{
    public override string ToString()
    {
        return SkuCode;
    }
}