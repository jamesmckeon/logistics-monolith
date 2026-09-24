using Throughline.Modules.Inventory.Infrastructure.Db;
using Throughline.Modules.Ordering.Contracts.Events;
using Wolverine.Attributes;

namespace Throughline.Modules.Inventory.Infrastructure.Events;

[Transactional(typeof(InventoryDbContext))]
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