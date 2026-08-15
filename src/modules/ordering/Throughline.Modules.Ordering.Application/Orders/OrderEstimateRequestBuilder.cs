using Throughline.Modules.Ordering.Application.Orders.Models;
using Throughline.Modules.Ordering.Domain.OrderEstimates;
using Throughline.Modules.Ordering.Domain.Pricing;
using Throughline.Modules.Ordering.Domain.Skus;

namespace Throughline.Modules.Ordering.Application.Orders;

public sealed class OrderEstimateRequestBuilder : IOrderEstimateRequestBuilder
{
    private readonly IMerchantRateQuery _merchantRateQuery;
    private readonly IPickFeeQuery _pickFeeQuery;
    private readonly ISkuAttributesQuery _skuAttributesQuery;
    private readonly IZoneChargeQuery _zoneChargeQuery;

    public OrderEstimateRequestBuilder(
        ISkuAttributesQuery skuAttributesQuery,
        IZoneChargeQuery zoneChargeQuery,
        IPickFeeQuery pickFeeQuery,
        IMerchantRateQuery merchantRateQuery)
    {
        _skuAttributesQuery = skuAttributesQuery;
        _zoneChargeQuery = zoneChargeQuery;
        _pickFeeQuery = pickFeeQuery;
        _merchantRateQuery = merchantRateQuery;
    }

    public Task<Result<OrderEstimateRequest>> CreateRequestAsync(CreateOrderCommand command)
    {
        throw new NotImplementedException();
    }
}