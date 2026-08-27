using Throughline.Modules.Billing.Domain.Models;

namespace Throughline.Modules.Billing.Domain.Pricing;

public sealed class MerchantPickFee
{
    public MerchantPickFee(SkuCode skuCode, Rate pickFee, int merchantId)
    {
        ArgumentNullException.ThrowIfNull(skuCode);
        ArgumentNullException.ThrowIfNull(pickFee);

        SkuCode = skuCode;
        PickFee = pickFee;
        MerchantId = merchantId;
    }

    public int MerchantId { get; }
    public SkuCode SkuCode { get; }
    public Rate PickFee { get; }
}