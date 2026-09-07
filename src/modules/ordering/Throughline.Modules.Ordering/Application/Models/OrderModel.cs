using Throughline.Modules.Ordering.Domain.Orders;

namespace Throughline.Modules.Ordering.Application.Models;

public sealed record OrderModel(
    Guid OrderId,
    int OwnerId,
    string PurchaseOrderNumber,
    string ReferenceNumber,
    DestinationModel Destination,
    IReadOnlyCollection<OrderLineModel> OrderLines)
{
    internal static OrderModel FromOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return new OrderModel(
            order.Id.Value,
            order.OwnerReferenceNumber.OwnerId,
            order.Content.PurchaseOrderNumber,
            order.OwnerReferenceNumber.ReferenceNumber,
            DestinationModel.FromStreetAddress(order.Content.Destination),
            order.Content.OrderLines.Select(ol =>
                    new OrderLineModel(
                        ol.SkuCode.Value,
                        ol.Quantity))
                .ToList().AsReadOnly()
        );
    }
}