using Throughline.Modules.Ordering.Domain.Models;

namespace Throughline.Modules.Ordering.Domain.Pricing;

public sealed class SkuPickFee
{
    public SkuCode SkuCode { get; }
    public Rate PickFee { get; }

    public SkuPickFee(SkuCode skuCode, Rate pickFee)
    {
        ArgumentNullException.ThrowIfNull(skuCode);
        ArgumentNullException.ThrowIfNull(pickFee);

        SkuCode = skuCode;
        PickFee = pickFee;
    }
}