using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.Pricing;
using Throughline.Modules.Ordering.Domain.Skus;

namespace Throughline.Modules.Ordering.Domain.OrderEstimates;

public sealed class OrderEstimateRequest
{
    public OrderEstimateRequest(
        PostalCode destinationCode,
        Rate handlingRate,
        IEnumerable<(MerchantSkuCode MerchantSku, int Quantity)> items,
        IEnumerable<ZoneSurcharge> zoneCharges)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (!items.Any())
            throw new ArgumentException("items must contain at least one member");

        ArgumentNullException.ThrowIfNull(zoneCharges);

        if (!zoneCharges.Any())
            throw new ArgumentException("zoneCharges must contain at least one member");

        ArgumentNullException.ThrowIfNull(destinationCode);
        ArgumentNullException.ThrowIfNull(handlingRate);

        Items = items;
        DestinationCode = destinationCode;
        HandlingRate = handlingRate;
        ZoneCharges = zoneCharges;
    }

    public IEnumerable<(MerchantSkuCode MerchantSku, int Quantity)> Items { get; }
    public PostalCode DestinationCode { get; }
    public Rate HandlingRate { get; }
    public IEnumerable<ZoneSurcharge> ZoneCharges { get; }
}