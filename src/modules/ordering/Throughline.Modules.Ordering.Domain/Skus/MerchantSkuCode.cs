using Throughline.Modules.Ordering.Domain.Models;

namespace Throughline.Modules.Ordering.Domain.Skus;

public sealed class MerchantSkuCode : ValueObject
{
    public MerchantSkuCode(int merchantId, SkuCode skuCode)
    {
        ArgumentNullException.ThrowIfNull(skuCode);

        MerchantId = merchantId;
        SkuCode = skuCode;
    }

    public int MerchantId { get; }
    public SkuCode SkuCode { get; }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return MerchantId;
        yield return SkuCode;
    }
}