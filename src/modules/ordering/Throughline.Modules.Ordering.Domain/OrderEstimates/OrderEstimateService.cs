using Throughline.Modules.Ordering.Domain.Pricing;

namespace Throughline.Modules.Ordering.Domain.OrderEstimates;

public class OrderEstimateService : IOrderEstimateService
{
    public Result<OrderEstimate> GetEstimate(OrderEstimateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<Error>();

        var zoneCharge = request.ZoneCharges.SingleOrDefault(s =>
            s.PostalZone.Includes(request.DestinationCode));

        if (zoneCharge == null)
            errors.Add(Error.Unexpected(
                $"Unable to locate a surcharge for postal code {request.DestinationCode}"));

        foreach (var skuCode in request.Items.Select(i => i.SkuCode).Distinct())
            if (request.PickFees.All(pf => pf.SkuCode != skuCode))
                errors.Add(Error.Unexpected(
                    $"Unable to locate a pick fee for sku {skuCode}"));

        if (errors.Any())
            return Result<OrderEstimate>.Failure(errors);

        var items = new List<OrderEstimateItem>();
        var totalCharge = new Money(0);

        foreach (var item in request.Items)
        {
            var pickFee = request.PickFees.Single(s => s.SkuCode == item.SkuCode).PickFee;
            var totalWeight = item.Quantity * item.Weight;

            var estimateItem = new OrderEstimateItem(
                item.SkuCode,
                item.Quantity,
                pickFee,
                Money.FromRate(item.Quantity, pickFee),
                totalWeight,
                Money.FromRate(totalWeight, request.HandlingRate));

            items.Add(estimateItem);
            totalCharge += estimateItem.TotalHandling + estimateItem.TotalHandling;
        }

        var surcharge = request.ZoneCharges.Single(s =>
            s.PostalZone.Includes(request.DestinationCode)).Surcharge;

        return Result<OrderEstimate>.Success(
            new OrderEstimate(surcharge, totalCharge, request.HandlingRate, items));
    }
}