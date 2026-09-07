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

    public async Task<Order?> GetOrderByOwnerReference(OwnerReferenceNumber ownerReferenceNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ownerReferenceNumber);

        var orderRecord = await _dbContext.Orders.SingleOrDefaultAsync(a =>
                a.OwnerId == ownerReferenceNumber.OwnerId && a.ReferenceNumber == ownerReferenceNumber.ReferenceNumber,
            cancellationToken);

        return orderRecord?.ToOrder();
    }
}