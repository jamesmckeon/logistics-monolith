using Throughline.Modules.Ordering.Domain.Orders;

namespace Throughline.Modules.Ordering.Application.Orders.Models;

public sealed record OrderModel(
    Guid OrderId,
    int MerchantId,
    string PurchaseOrderNumber,
    string ReferenceNumber,
    DestinationModel Destination,
    decimal DestinationSurcharge,
    decimal TotalCharges,
    IReadOnlyCollection<OrderLineModel> OrderLines)
{
    public static OrderModel FromOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return new(
            order.Id.Value,
            order.MerchantId,
            order.PurchaseOrderNumber,
            order.ReferenceNumber,
            new DestinationModel(
                order.Destination.StreeAddressOne,
                order.Destination.StreetAddressTwo,
                order.Destination.City,
                order.Destination.State.Value,
                order.Destination.ZipCode.Value),
            order.DestinationSurcharge.Value,
            order.TotalCharges.Value,
            order.OrderLines.Select(ol =>
                    new OrderLineModel(
                        ol.SkuCode.Value,
                        ol.TotalQuantity,
                        ol.UnitPickFee.Value,
                        ol.TotalPickFees.Value,
                        ol.TotalWeight,
                        ol.TotalHandling.Value))
                .ToList().AsReadOnly()
        );
    }
}