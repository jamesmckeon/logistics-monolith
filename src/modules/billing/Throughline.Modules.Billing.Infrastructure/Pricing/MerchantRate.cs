using Throughline.Modules.Billing.Domain.Models;

namespace Throughline.Modules.Billing.Infrastructure.Pricing;

internal sealed class MerchantRate
{
    public int MerchantId { get; set; }
    public required Rate Rate { get; set; }
}