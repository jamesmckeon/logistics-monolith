namespace Throughline.Modules.Ordering.Domain.Pricing;

public interface IZoneChargeQuery
{
    Task<IEnumerable<ZoneSurcharge>> GetChargesAsync(int merchantId);
}