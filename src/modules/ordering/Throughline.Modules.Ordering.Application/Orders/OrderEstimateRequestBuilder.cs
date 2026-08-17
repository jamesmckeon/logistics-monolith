using Throughline.Modules.Ordering.Application.Orders.Models;
using Throughline.Modules.Ordering.Domain.Models;
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

    public async Task<Result<OrderEstimateRequest>> CreateRequestAsync(CreateOrderCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var quantityBySku = ConsolidateBySku(command.Items);

        var pickFeeBySku = (await _pickFeeQuery.GetPickFeesAsync(command.MerchantId))
            .ToDictionary(f => f.SkuCode, f => f.PickFee);

        var weightBySku = (await _skuAttributesQuery.GetAttributesAsync(command.MerchantId, quantityBySku.Keys))
            .ToDictionary(a => a.SkuCode, a => a.Weight);

        var items = new List<OrderEstimateRequestItem>();
        foreach (var (skuCode, totalQuantity) in quantityBySku)
        {
            if (!pickFeeBySku.TryGetValue(skuCode, out var pickFee))
                return Unavailable($"No pick fee for SKU '{skuCode}' (merchant {command.MerchantId}).");

            if (!weightBySku.TryGetValue(skuCode, out var unitWeight))
                return Unavailable($"No attributes for SKU '{skuCode}' (merchant {command.MerchantId}).");

            items.Add(new OrderEstimateRequestItem(skuCode, totalQuantity, unitWeight, pickFee));
        }

        var destination = new PostalCode(command.PostalCode);
        var zoneCharge = (await _zoneChargeQuery.GetChargesAsync(command.MerchantId))
            .FirstOrDefault(z => z.PostalZone.Includes(destination));
        if (zoneCharge is null)
            return Unavailable(
                $"No zone surcharge covers postal code '{destination}' (merchant {command.MerchantId}).");

        var handlingRate = await _merchantRateQuery.GetHandlingAsync(command.MerchantId);
        if (handlingRate is null)
            return Unavailable($"No handling rate for merchant {command.MerchantId}.");

        return new OrderEstimateRequest(handlingRate, items, zoneCharge);
    }

    private static Dictionary<SkuCode, int> ConsolidateBySku(
        IEnumerable<(string Sku, int Quantity)> orderItems)
    {
        var quantityBySku = new Dictionary<SkuCode, int>();
        foreach (var item in orderItems)
        {
            var skuCode = new SkuCode(item.Sku);
            quantityBySku[skuCode] = quantityBySku.GetValueOrDefault(skuCode) + item.Quantity;
        }

        return quantityBySku;
    }

    private static Result<OrderEstimateRequest> Unavailable(string description)
    {
        return Result<OrderEstimateRequest>.Failure(Error.Unavailable(description));
    }
}