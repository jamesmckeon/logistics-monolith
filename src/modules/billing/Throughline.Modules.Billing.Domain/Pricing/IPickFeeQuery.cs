namespace Throughline.Modules.Billing.Domain.Pricing;

public interface IPickFeeQuery
{
    Task<IEnumerable<MerchantPickFee>> GetPickFeesAsync(
        int merchantId, CancellationToken cancellationToken = default);
}