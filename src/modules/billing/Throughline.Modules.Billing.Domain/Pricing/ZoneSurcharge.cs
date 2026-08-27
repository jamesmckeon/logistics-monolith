using Throughline.Modules.Billing.Domain.Models;

namespace Throughline.Modules.Billing.Domain.Pricing;

public sealed class ZoneSurcharge
{
    public ZoneSurcharge(
        int merchantId,
        PostalZone postalZone,
        Money surcharge)
    {
        ArgumentNullException.ThrowIfNull(postalZone);
        ArgumentNullException.ThrowIfNull(surcharge);

        MerchantId = merchantId;
        PostalZone = postalZone;
        Surcharge = surcharge;
    }

    public int MerchantId { get; }
    public PostalZone PostalZone { get; }
    public Money Surcharge { get; }
}