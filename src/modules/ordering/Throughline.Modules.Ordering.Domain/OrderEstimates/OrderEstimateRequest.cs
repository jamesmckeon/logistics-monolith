using Throughline.Common.Collections;
using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.Pricing;

namespace Throughline.Modules.Ordering.Domain.OrderEstimates;

public sealed class OrderEstimateRequest
{
    public OrderEstimateRequest(
        PostalCode destinationCode,
        Rate handlingRate,
        IEnumerable<OrderEstimateRequestItem> items,
        IEnumerable<ZoneSurcharge> zoneCharges,
        IEnumerable<(CaseInsensitiveString Sku, Rate PickFee)> pickFees)
    {
        ArgumentNullException.ThrowIfNull(destinationCode);
        ArgumentNullException.ThrowIfNull(handlingRate);

        DestinationCode = destinationCode;
        HandlingRate = handlingRate;
        Items = items.ToNonEmptyArray();
        ZoneCharges = zoneCharges.ToNonEmptyArray();
        PickFees = pickFees.ToNonEmptyArray();
    }

    public IEnumerable<OrderEstimateRequestItem> Items { get; }
    public IEnumerable<(CaseInsensitiveString SkuCode, Rate PickFee)> PickFees { get; }
    public PostalCode DestinationCode { get; }
    public Rate HandlingRate { get; }
    public IEnumerable<ZoneSurcharge> ZoneCharges { get; }
}