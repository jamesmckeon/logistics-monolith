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
        List<OrderLine> orderLines,
        Money destinationSurcharge,
        Money totalCharges,
        decimal totalWeight)
    {
        Id = id;
        MerchantId = merchantId;
        PurchaseOrderNumber = purchaseOrderNumber.Trim();
        ReferenceNumber = referenceNumber.Trim();
        Destination = destination;
        OrderLines = orderLines;
        DestinationSurcharge = destinationSurcharge;
        TotalCharges = totalCharges;
        TotalWeight = totalWeight;
    }

    public OrderId Id { get; }
    public int MerchantId { get; }
    public string PurchaseOrderNumber { get; }
    public string ReferenceNumber { get; }
    public StreetAddress Destination { get; }
    public Money DestinationSurcharge { get; }
    public Money TotalCharges { get; }
    public IReadOnlyCollection<OrderLine> OrderLines { get; }
    public decimal TotalWeight { get; }

    public static Order FromOrderEstimate(
        OrderId orderId,
        OrderEstimate orderEstimate,
        int merchantId,
        string purchaseOrderNumber,
        string referenceNumber,
        StreetAddress destination)
    {
        ArgumentNullException.ThrowIfNull(orderId);
        ArgumentNullException.ThrowIfNull(orderEstimate);
        ArgumentNullException.ThrowIfNull(destination);

        ArgumentNullException.ThrowIfNull(purchaseOrderNumber);
        ArgumentNullException.ThrowIfNull(referenceNumber);

        var orderLines = orderEstimate.Items.Select(i => new OrderLine(
                i.Quantity, i.SkuCode, i.PickFeeRate, i.TotalPickFee, i.Weight, i.TotalHandling))
            .ToList();

        return new Order(
            orderId,
            merchantId,
            purchaseOrderNumber,
            referenceNumber,
            destination,
            orderLines,
            orderEstimate.ZoneSurcharge,
            orderEstimate.TotalCharge,
            orderLines.Sum(s => s.TotalWeight));
    }
}