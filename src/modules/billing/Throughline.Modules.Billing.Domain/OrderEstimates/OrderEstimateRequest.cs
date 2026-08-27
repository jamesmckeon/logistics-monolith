using Throughline.Common.Collections;
using Throughline.Modules.Billing.Domain.Models;
using Throughline.Modules.Billing.Domain.Pricing;

namespace Throughline.Modules.Billing.Domain.OrderEstimates;

public sealed class OrderEstimateRequest
{
    public OrderEstimateRequest(
        Rate handlingRate,
        IEnumerable<OrderEstimateRequestItem> items,
        ZoneSurcharge zoneCharge)
    {
        ArgumentNullException.ThrowIfNull(zoneCharge);
        ArgumentNullException.ThrowIfNull(handlingRate);

        Items = items.ToNonEmptyArray();
        HandlingRate = handlingRate;
        ZoneCharge = zoneCharge;
    }

    public IEnumerable<OrderEstimateRequestItem> Items { get; }
    public Rate HandlingRate { get; }
    public ZoneSurcharge ZoneCharge { get; }
}