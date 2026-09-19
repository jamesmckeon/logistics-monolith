using Microsoft.Extensions.Logging;
using Throughline.Modules.Inventory.Infrastructure.Orders;
using Throughline.Modules.Ordering.Contracts.Events;

namespace Throughline.Modules.Inventory.Domain.Orders;

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

        var lines = message.Lines.Select(l => new OrderLine(l.SkuCode, l.QuantityRequested));
        await ordersRepository.SaveConfirmedOrder(new(id, lines, OrderAllocationStatus.NotAllocated), token);

        logger.LogInformation(
            "Saved new confirmed order for owner {OwnerId}, order {OrderId}",
            message.OwnerId, message.OrderId);
    }
}