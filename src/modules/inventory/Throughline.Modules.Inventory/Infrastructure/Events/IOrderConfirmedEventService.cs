using Throughline.Modules.Ordering.Contracts.Events;

namespace Throughline.Modules.Inventory.Infrastructure.Events;

public interface IOrderConfirmedEventService
{
    Task HandleMessageAsync(OrderConfirmedIntegrationEvent message, CancellationToken token);
}