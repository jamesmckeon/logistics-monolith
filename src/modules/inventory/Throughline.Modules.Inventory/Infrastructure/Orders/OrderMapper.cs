using Throughline.Modules.Inventory.Orders;

namespace Throughline.Modules.Inventory.Infrastructure.Orders;

internal static class OrderMapper
{
    public static OrderRecord ToRecord(this Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        var ownerId = order.OwnerOrderId.OwnerId;
        var orderId = order.OwnerOrderId.OrderId;

        return new OrderRecord
        {
            OwnerId = ownerId,
            OrderId = orderId,
            OrderLines = order.OrderLines
                .Select(l => new OrderLineRecord
                {
                    OwnerId = ownerId,
                    OrderId = orderId,
                    SkuCode = l.SkuCode,
                    QuantityRequested = l.QuantityRequested
                })
                .ToList()
        };
    }

    public static Order ToOrder(this OrderRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var lines = record.OrderLines
            .Select(l => new OrderLine(l.SkuCode, l.QuantityRequested));

        return new Order(new OwnerOrderId(record.OwnerId, record.OrderId), lines);
    }
}
