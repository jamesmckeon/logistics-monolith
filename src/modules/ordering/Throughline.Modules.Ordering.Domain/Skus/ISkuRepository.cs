using Throughline.Modules.Ordering.Domain.Models;

namespace Throughline.Modules.Ordering.Domain.Skus;

public interface ISkuRepository
{
    Task<IEnumerable<SkuAttributes>> GetAttributesByMerchantCodesAsync(
        int merchantId, IEnumerable<SkuCode> skuCodes);
}