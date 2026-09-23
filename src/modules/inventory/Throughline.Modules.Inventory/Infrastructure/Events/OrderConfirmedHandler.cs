using Throughline.Modules.Ordering.Contracts.Events;

namespace Throughline.Modules.Inventory.Infrastructure.Events;

public sealed class OrderConfirmedHandler
{
    public async Task Handle(
        OrderConfirmedIntegrationEvent message,
        IOrderConfirmedEventService eventService,
        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(eventService);

        await eventService.HandleMessageAsync(message, token);
    }
}