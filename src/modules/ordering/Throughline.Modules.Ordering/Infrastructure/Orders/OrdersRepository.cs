using Microsoft.EntityFrameworkCore;
using Throughline.Modules.Ordering.Domain.Orders;

namespace Throughline.Modules.Ordering.Infrastructure.Orders;

internal sealed class OrdersRepository
{
    private readonly OrdersDbContext _dbContext;

    public OrdersRepository(OrdersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> SaveOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        var record = order.ToOrderRecord();
        _dbContext.Orders.Add(record);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return record.OrderId;
    }

    public async Task<Guid?> GetOrderId(int ownerId, string referenceNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(referenceNumber);

        var order = await _dbContext.Orders.SingleOrDefaultAsync(a =>
                a.OwnerId == ownerId && a.ReferenceNumber == referenceNumber,
            cancellationToken);

        return order?.OrderId;
    }
}