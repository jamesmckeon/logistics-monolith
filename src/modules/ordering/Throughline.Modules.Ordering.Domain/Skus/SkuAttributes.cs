using Throughline.Modules.Ordering.Domain.Models;

namespace Throughline.Modules.Ordering.Domain.Skus;

public sealed class SkuAttributes : ValueObject
{
    public SkuAttributes(MerchantSkuCode merchantSkuCode, Rate pickFee, Rate weight)
    {
        ArgumentNullException.ThrowIfNull(merchantSkuCode);
        ArgumentNullException.ThrowIfNull(pickFee);
        ArgumentNullException.ThrowIfNull(weight);

        MerchantSkuCode = merchantSkuCode;
        PickFee = pickFee;
        Weight = weight;
    }

    public MerchantSkuCode MerchantSkuCode { get; }
    public Rate PickFee { get; }
    public Rate Weight { get; }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return MerchantSkuCode;
    }
}