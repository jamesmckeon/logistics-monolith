using Throughline.Modules.Ordering.Domain.Models;

namespace Throughline.Modules.Ordering.Domain.Skus;

public sealed class MerchantSkuCode : ValueObject
{
    public MerchantSkuCode(int merchantId, string skuCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skuCode);

        MerchantId = merchantId;
        SkuCode = skuCode.Trim();
    }

    public int MerchantId { get; }
    public string SkuCode { get; }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return MerchantId;
        yield return SkuCode.ToUpperInvariant();
    }
}