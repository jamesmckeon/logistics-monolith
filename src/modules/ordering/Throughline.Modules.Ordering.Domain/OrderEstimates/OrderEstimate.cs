using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.Pricing;

namespace Throughline.Modules.Ordering.Domain.OrderEstimates;

public sealed record OrderEstimate
{
    internal OrderEstimate(
        Money zoneSurcharge,
        Money totalCharge,
        Rate handlingRate,
        IEnumerable<OrderEstimateItem> items)
    {
        ZoneSurcharge = zoneSurcharge;
        TotalCharge = totalCharge;
        HandlingRate = handlingRate;
        Items = items;
    }

    public Money ZoneSurcharge { get; }
    public Money TotalCharge { get; }
    public Rate HandlingRate { get; }
    public IEnumerable<OrderEstimateItem> Items { get; }
}