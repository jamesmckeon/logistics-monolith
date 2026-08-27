using Throughline.Modules.Billing.Domain.Models;

namespace Throughline.Modules.Billing.Domain.Skus;

public sealed class SkuAttributes
{
    public SkuAttributes(SkuCode skuCode, decimal weight)
    {
        ArgumentNullException.ThrowIfNull(skuCode);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(weight, 0);

        SkuCode = skuCode;
        Weight = weight;
    }

    public SkuCode SkuCode { get; }
    public decimal Weight { get; }
}