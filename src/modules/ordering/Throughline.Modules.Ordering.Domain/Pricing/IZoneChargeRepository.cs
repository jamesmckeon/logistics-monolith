namespace Throughline.Modules.Ordering.Domain.Pricing;

public interface IZoneChargeRepository
{
    Task<IEnumerable<ZoneSurcharge>> GetChargesByMerchantIdAsync(int merchantId);
}