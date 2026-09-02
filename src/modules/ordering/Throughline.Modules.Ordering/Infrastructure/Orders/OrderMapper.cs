using Throughline.Modules.Ordering.Domain;
using Throughline.Modules.Ordering.Domain.Orders;

namespace Throughline.Modules.Ordering.Infrastructure.Orders;

internal static class OrderMapper
{
    public static OrderRecord ToOrderRecord(this Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return new OrderRecord
        {
            OrderId = order.Id.Value,
            MerchantId = order.MerchantId,
            PurchaseOrderNumber = order.PurchaseOrderNumber,
            ReferenceNumber = order.ReferenceNumber,
            StreetAddressOne = order.Destination.StreeAddressOne,
            StreetAddressTwo = order.Destination.StreetAddressTwo,
            City = order.Destination.City,
            State = order.Destination.State,
            Zipcode = order.Destination.ZipCode.Value,
            OrderLines = order.OrderLines
                .Select(l => new OrderLineRecord
                {
                    OrderId = order.Id.Value,
                    SkuCode = l.SkuCode.Value,
                    Quantity = l.Quantity
                })
                .ToList()
        };
    }

    public static Order ToOrder(this OrderRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var destination = new StreetAddress(
            record.StreetAddressOne,
            record.StreetAddressTwo,
            record.City,
            record.State,
            new PostalCode(record.Zipcode));

        var orderLines = record.OrderLines
            .Select(l => new OrderLine(new SkuCode(l.SkuCode), l.Quantity));

        // Rehydration bypasses Order.Create: the row was validated on write, so we rebuild the
        // aggregate directly rather than re-running (or failing) validation on read.
        return new Order(
            new OrderId(record.OrderId),
            record.MerchantId,
            record.PurchaseOrderNumber,
            record.ReferenceNumber,
            destination,
            orderLines);
    }
}