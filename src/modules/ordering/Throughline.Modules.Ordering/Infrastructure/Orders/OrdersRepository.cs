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

    public async Task SaveOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        _dbContext.Orders.Add(order.ToOrderRecord());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> OrderExistsFor(int merchantId, string referenceNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(referenceNumber);

        return await _dbContext.Orders.AnyAsync(a =>
                a.MerchantId == merchantId && a.ReferenceNumber == referenceNumber,
            cancellationToken);
    }
}