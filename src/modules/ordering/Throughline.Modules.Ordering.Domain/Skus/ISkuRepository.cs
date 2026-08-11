namespace Throughline.Modules.Ordering.Domain.Skus;

public interface ISkuRepository
{
    Task<IEnumerable<SkuAttributes>> GetAttributesByCodesAsync(IEnumerable<MerchantSkuCode> skuCodes);
}