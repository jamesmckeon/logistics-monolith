using Throughline.Modules.Ordering.Domain;

namespace Throughline.Modules.Ordering.Models;

public sealed record OrderModel(
    Guid OrderId,
    int MerchantId,
    string PurchaseOrderNumber,
    string ReferenceNumber,
    DestinationModel Destination,
    IReadOnlyCollection<OrderLineModel> OrderLines)
{
    public static OrderModel FromOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return new OrderModel(
            order.Id.Value,
            order.MerchantId,
            order.PurchaseOrderNumber,
            order.ReferenceNumber,
            new DestinationModel(
                order.Destination.StreeAddressOne,
                order.Destination.StreetAddressTwo,
                order.Destination.City,
                order.Destination.State,
                order.Destination.ZipCode.Value),
            order.OrderLines.Select(ol =>
                    new OrderLineModel(
                        ol.SkuCode.Value,
                        ol.Quantity))
                .ToList().AsReadOnly()
        );
    }
}