using Throughline.Modules.Ordering.Domain.Common.Exceptions;
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
        var itemsArr = items.ToArray();
        EmptyCollectionException.ThrowIfNullOrEmpty(itemsArr, nameof(items));

        var chargesArray = zoneCharges.ToArray();
        EmptyCollectionException.ThrowIfNullOrEmpty(chargesArray, nameof(zoneCharges));

        (CaseInsensitiveString SkuCode, Rate PickFee)[]? feesArr = pickFees.ToArray();
        EmptyCollectionException.ThrowIfNullOrEmpty(feesArr, nameof(pickFees));

        ArgumentNullException.ThrowIfNull(destinationCode);
        ArgumentNullException.ThrowIfNull(handlingRate);

        Items = itemsArr;
        DestinationCode = destinationCode;
        HandlingRate = handlingRate;
        ZoneCharges = chargesArray;
        PickFees = feesArr;
    }

    public IEnumerable<OrderEstimateRequestItem> Items { get; }
    public IEnumerable<(CaseInsensitiveString SkuCode, Rate PickFee)> PickFees { get; }
    public PostalCode DestinationCode { get; }
    public Rate HandlingRate { get; }
    public IEnumerable<ZoneSurcharge> ZoneCharges { get; }
}