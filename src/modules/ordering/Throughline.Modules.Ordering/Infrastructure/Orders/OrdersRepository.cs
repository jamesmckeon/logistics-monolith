using Microsoft.EntityFrameworkCore;
using Npgsql;
using Throughline.Modules.Ordering.Domain.Orders;

namespace Throughline.Modules.Ordering.Infrastructure.Orders;

internal sealed record SaveOrderResult(OrderId OrderId, bool Created)
{
}

internal sealed class OrdersRepository
{
    private readonly OrdersDbContext _dbContext;

    public OrdersRepository(OrdersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SaveOrderResult> SaveOrderAsync(Order order,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        var record = order.ToOrderRecord();
        _dbContext.Orders.Add(record);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
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