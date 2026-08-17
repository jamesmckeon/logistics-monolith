namespace Throughline.Modules.Ordering.Domain.Pricing;

public interface IPickFeeQuery
{
    Task<IEnumerable<SkuPickFee>> GetPickFeesAsync(int merchantId, CancellationToken cancellationToken = default);
}