namespace Throughline.Modules.Ordering.Domain.Pricing;

public interface IPickFeeQuery
{
    Task<IEnumerable<MerchantPickFee>> GetPickFeesAsync(
        int merchantId, CancellationToken cancellationToken = default);
}