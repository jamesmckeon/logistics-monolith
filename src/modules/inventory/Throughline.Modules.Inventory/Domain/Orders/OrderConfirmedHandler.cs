using Microsoft.Extensions.Logging;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Infrastructure.Common;
using Throughline.Modules.Ordering.Contracts.Events;

namespace Throughline.Modules.Inventory.Domain.Orders;

public sealed class OrderConfirmedHandler
{
    public async Task Handle(
        OrderConfirmedIntegrationEvent message,
        IOrderAllocationRepository orderAllocationRepository,
        ILogger<OrderConfirmedHandler> logger,
        IUnitOfWork unitOfWork,
        CancellationToken token)
    {
        logger.LogInformation(
            "Received OrderConfirmed for owner {OwnerId}, order {OrderId}",
            message.OwnerId, message.OrderId);

        var lines = message.Lines.Select(l => new OrderLineAllocation(l.SkuCode, l.QuantityRequested));
        var orderResult =
            OrderAllocation.Create(message.OwnerId, message.OrderId, lines);

        // TODO: should an exception be thrown so WOlverine retries?

        if (!orderResult.Succeeded)
        {
            logger.LogError($"Unable to create order allocation: {orderResult.Errors.First()}");
        }
        else
        {
            orderAllocationRepository.Add(orderResult.Value);
            await unitOfWork.SaveChangesAsync();

            logger.LogInformation(
                "Saved new confirmed order for owner {OwnerId}, order {OrderId}",
                message.OwnerId, message.OrderId);
        }
    }
}