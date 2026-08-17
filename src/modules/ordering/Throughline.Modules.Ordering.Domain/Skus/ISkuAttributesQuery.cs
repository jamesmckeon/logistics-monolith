using Throughline.Modules.Ordering.Domain.Models;

namespace Throughline.Modules.Ordering.Domain.Skus;

public interface ISkuAttributesQuery
{
    Task<IEnumerable<SkuAttributes>> GetAttributesAsync(
        int merchantId, IEnumerable<SkuCode> skuCodes, CancellationToken cancellationToken = default);
}