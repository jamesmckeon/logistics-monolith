using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.OrderEstimates;
using Throughline.Modules.Ordering.Domain.Pricing;

namespace Throughline.Modules.Ordering.Domain.Orders;

public sealed class Order
{
    private Order(
        OrderId id,
        int merchantId,
        string purchaseOrderNumber,
        string referenceNumber,
        StreetAddress destination,
        IEnumerable<OrderLine> orderLines,
        Money destinationSurcharge,
        Money totalCharges)
    {
        Id = id;
        MerchantId = merchantId;
        PurchaseOrderNumber = purchaseOrderNumber.Trim();
        ReferenceNumber = referenceNumber.Trim();
        Destination = destination;
        OrderLines = orderLines;
        DestinationSurcharge = destinationSurcharge;
        TotalCharges = totalCharges;
    }

    public OrderId Id { get; }
    public int MerchantId { get; }
    public string PurchaseOrderNumber { get; }
    public string ReferenceNumber { get; }
    public StreetAddress Destination { get; }
    public Money DestinationSurcharge { get; }
    public Money TotalCharges { get; }
    public IEnumerable<OrderLine> OrderLines { get; }


    public static Order FromOrderEstimate(
        OrderEstimate orderEstimate,
        int merchantId,
        string purchaseOrderNumber,
        string referenceNumber,
        StreetAddress destination)
    {
        ArgumentNullException.ThrowIfNull(orderEstimate);
        ArgumentNullException.ThrowIfNull(destination);

        ArgumentNullException.ThrowIfNull(purchaseOrderNumber);
        ArgumentNullException.ThrowIfNull(referenceNumber);

        var orderLines = orderEstimate.Items.Select(i => new OrderLine(
            i.Quantity, i.SkuCode, i.PickFeeRate, i.TotalPickFee, i.Weight, i.TotalHandling));

        return new Order(
            new OrderId(),
            merchantId,
            purchaseOrderNumber,
            referenceNumber,
            destination,
            orderLines,
            orderEstimate.ZoneSurcharge,
            orderEstimate.TotalCharge);
    }
}