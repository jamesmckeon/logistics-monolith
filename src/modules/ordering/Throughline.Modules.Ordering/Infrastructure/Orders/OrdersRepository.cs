using Microsoft.EntityFrameworkCore;
using Npgsql;
using Throughline.Modules.Ordering.Contracts.Events;
using Throughline.Modules.Ordering.Domain.Orders;
using Wolverine.EntityFrameworkCore;

namespace Throughline.Modules.Ordering.Infrastructure.Orders;

internal sealed class OrdersRepository : IOrdersRepository
{
    private readonly OrdersDbContext _dbContext;
    private readonly IDbContextOutbox _outbox;

    public OrdersRepository(OrdersDbContext dbContext, IDbContextOutbox outbox)
    {
        _dbContext = dbContext;
        _outbox = outbox;
    }

    public async Task<SaveOrderResult> SaveOrderAsync(
        Order order,
        OrderConfirmedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        var record = order.ToOrderRecord();
        _dbContext.Orders.Add(record);

        _outbox.Enroll(_dbContext);
        await _outbox.PublishAsync(integrationEvent);

        try
        {
            await _outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
            return new SaveOrderResult(new OrderId(record.OrderId), true);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: OrderRecordConfiguration.OwnerReferenceIndexName
            })
        {
            var existingOrder = await _dbContext.Orders.SingleAsync(s =>
                    s.OwnerId == order.OwnerReferenceNumber.OwnerId &&
                    s.ReferenceNumber == order.OwnerReferenceNumber.ReferenceNumber,
                cancellationToken);

            return new SaveOrderResult(new OrderId(existingOrder.OrderId), false);
        }
    }

    public async Task<Order?> GetOrderByOwnerReference(OwnerReferenceNumber ownerReferenceNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ownerReferenceNumber);

        var orderRecord = await _dbContext.Orders
            .Include(i => i.OrderLines)
            .SingleOrDefaultAsync(a =>
                    a.OwnerId == ownerReferenceNumber.OwnerId &&
                    a.ReferenceNumber == ownerReferenceNumber.ReferenceNumber,
                cancellationToken);

        return orderRecord?.ToOrder();
    }
}