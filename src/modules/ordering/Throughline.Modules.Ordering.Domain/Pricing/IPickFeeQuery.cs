namespace Throughline.Modules.Ordering.Domain.Pricing;

public interface IPickFeeRepository
{
    Task<IEnumerable<SkuPickFee>> GetPickFeesAsync(int merchantId);
}