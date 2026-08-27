using Throughline.Modules.Billing.Domain.Models;

namespace Throughline.Modules.Billing.Domain.Pricing;

public interface IMerchantRateQuery
{
    Task<Rate?> GetHandlingAsync(int merchantId, CancellationToken cancellationToken = default);
}