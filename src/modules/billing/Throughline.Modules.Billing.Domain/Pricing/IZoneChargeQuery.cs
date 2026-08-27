namespace Throughline.Modules.Billing.Domain.Pricing;

public interface IZoneChargeQuery
{
    Task<IEnumerable<ZoneSurcharge>> GetChargesAsync(int merchantId, CancellationToken cancellationToken = default);
}