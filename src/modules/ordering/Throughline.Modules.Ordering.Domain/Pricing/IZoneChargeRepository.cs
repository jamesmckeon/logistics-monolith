namespace Throughline.Modules.Ordering.Domain.Models;

public interface IZoneChargeRepository
{
    Task<IEnumerable<ZoneSurCharge>> GetChargesByMerchantIdAsync(int merchantId);
}