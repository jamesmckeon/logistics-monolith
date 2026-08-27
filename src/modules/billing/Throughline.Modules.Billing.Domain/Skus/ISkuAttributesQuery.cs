using Throughline.Modules.Billing.Domain.Models;

namespace Throughline.Modules.Billing.Domain.Skus;

public interface ISkuAttributesQuery
{
    Task<IEnumerable<SkuAttributes>> GetAttributesAsync(
        int merchantId, IEnumerable<SkuCode> skuCodes, CancellationToken cancellationToken = default);
}