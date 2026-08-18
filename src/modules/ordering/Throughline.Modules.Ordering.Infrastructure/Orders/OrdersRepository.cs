using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Throughline.Modules.Ordering.Domain.Orders;

namespace Throughline.Modules.Ordering.Infrastructure.Orders;

public sealed class OrdersRepository : DbContext, IOrdersRepository
{
    private readonly ILogger<OrdersRepository> _logger;

    public OrdersRepository(
        DbContextOptions<OrdersRepository> options, ILogger<OrdersRepository> logger) : base(options)
    {
        _logger = logger;
    }

    public DbSet<Order> Orders => Set<Order>();

    public Task SaveOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        throw new NotImplementedException();
    }

    public async Task<bool> OrderExistsFor(int merchantId, string referenceNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(referenceNumber);

        return await Orders.AnyAsync(a =>
                a.MerchantId == merchantId && a.ReferenceNumber == referenceNumber,
            cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersRepository).Assembly);
    }
}