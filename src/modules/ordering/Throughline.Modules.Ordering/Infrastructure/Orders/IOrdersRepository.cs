using Throughline.Modules.Ordering.Contracts.Events;
using Throughline.Modules.Ordering.Domain.Orders;

namespace Throughline.Modules.Ordering.Infrastructure.Orders;

internal interface IOrdersRepository
{
    Task<Order?> GetOrderByOwnerReference(OwnerReferenceNumber ownerReferenceNumber,
        CancellationToken cancellationToken);

    Task<SaveOrderResult> SaveOrderAsync(
        Order order,
        OrderConfirmedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken);
}