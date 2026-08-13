using Throughline.Modules.Ordering.Domain.Pricing;

namespace Throughline.Modules.Ordering.Domain.OrderEstimates;

public class OrderEstimateService : IOrderEstimateService
{
    public Result<OrderEstimate> GetEstimate(OrderEstimateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var items = new List<OrderEstimateItem>();
        var totalCharge = new Money(0);

        foreach (var item in request.Items)
        {
            var totalWeight = item.TotalQuantity * item.UnitWeight;

            var estimateItem = new OrderEstimateItem(
                item.SkuCode,
                item.TotalQuantity,
                item.UnitPickFee,
                Money.FromRate(item.TotalQuantity, item.UnitPickFee),
                totalWeight,
                Money.FromRate(totalWeight, request.HandlingRate));

            items.Add(estimateItem);
            totalCharge += estimateItem.TotalPickFee + estimateItem.TotalHandling;
        }

        totalCharge += request.ZoneCharge.Surcharge;

        return Result<OrderEstimate>.Success(
            new OrderEstimate(request.ZoneCharge.Surcharge, totalCharge, request.HandlingRate, items));
    }
}