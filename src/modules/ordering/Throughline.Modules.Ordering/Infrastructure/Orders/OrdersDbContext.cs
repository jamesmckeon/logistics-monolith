using Microsoft.EntityFrameworkCore;

namespace Throughline.Modules.Ordering.Infrastructure.Orders;

internal sealed class OrdersDbContext : DbContext
{
    internal OrdersDbContext(
        DbContextOptions<OrdersDbContext> options) : base(options)
    {
    }

    public DbSet<OrderRecord> Orders => Set<OrderRecord>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("orders");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
    }
}