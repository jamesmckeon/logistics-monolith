using Throughline.Modules.Ordering.Contracts.Events;

namespace Throughline.Modules.Inventory.Infrastructure.Orders;

public interface IOrderConfirmedEventService
{
    Task HandleMessageAsync(OrderConfirmedIntegrationEvent message, CancellationToken token);
}