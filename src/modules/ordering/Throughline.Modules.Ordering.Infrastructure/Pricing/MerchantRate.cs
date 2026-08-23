using Throughline.Modules.Ordering.Domain.Models;

namespace Throughline.Modules.Ordering.Infrastructure.Pricing;

internal sealed class MerchantRate
{
    public int MerchantId { get; set; }
    public required Rate Rate { get; set; }
}