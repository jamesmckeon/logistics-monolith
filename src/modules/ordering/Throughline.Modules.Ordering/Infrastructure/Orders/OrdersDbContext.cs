using Microsoft.EntityFrameworkCore;

namespace Throughline.Modules.Ordering.Infrastructure.Orders;

internal sealed class OrdersDbContext : DbContext
{
    // Public ctor is required by AddDbContext (EF resolves the context through DI); the type
    // itself stays internal, so the module boundary is unaffected.
    public OrdersDbContext(
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