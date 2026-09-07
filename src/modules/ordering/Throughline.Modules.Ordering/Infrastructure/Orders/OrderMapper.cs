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
            OwnerId = order.OwnerReferenceNumber.OwnerId,
            PurchaseOrderNumber = order.Content.PurchaseOrderNumber,
            ReferenceNumber = order.OwnerReferenceNumber.ReferenceNumber,
            StreetAddressOne = order.Content.Destination.StreeAddressOne,
            StreetAddressTwo = order.Content.Destination.StreetAddressTwo,
            City = order.Content.Destination.City,
            State = order.Content.Destination.State,
            Zipcode = order.Content.Destination.ZipCode.Value,
            OrderLines = order.Content.OrderLines
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

        return new Order(
            new OrderId(record.OrderId),
            new OwnerReferenceNumber(record.OwnerId, record.ReferenceNumber),
            new OrderContent(record.PurchaseOrderNumber, destination, orderLines));
    }
}