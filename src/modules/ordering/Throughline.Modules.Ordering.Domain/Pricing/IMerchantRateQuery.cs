using Throughline.Modules.Ordering.Domain.Models;

namespace Throughline.Modules.Ordering.Domain.Pricing;

public interface IMerchantRateQuery
{
    Task<Rate?> GetHandlingAsync(int merchantId, CancellationToken cancellationToken = default);
}