using Microsoft.Extensions.Logging;
using Throughline.Modules.Ordering.Contracts.Events;

namespace Throughline.Modules.Inventory.Orders;

public sealed class OrderConfirmedHandler
{
    public async Task Handle(
        OrderConfirmedIntegrationEvent message,
        IOrdersRepository ordersRepository,
        ILogger<OrderConfirmedHandler> logger,
        CancellationToken token)
    {
        logger.LogInformation(
            "Received OrderConfirmed for owner {OwnerId}, order {OrderId}",
            message.OwnerId, message.OrderId);

        var id = new OwnerOrderId(message.OwnerId, message.OrderId);
        var existing = await ordersRepository.GetByOwnerOrderIdAsync(id, token);
        var lines = message.Lines.Select(l => new OrderLine(l.SkuCode, l.QuantityRequested));
        await ordersRepository.SaveConfirmedOrder(new(id, lines), token);


        logger.LogInformation(
            "Saved new confirmed order for owner {OwnerId}, order {OrderId}",
            message.OwnerId, message.OrderId);
    }
}