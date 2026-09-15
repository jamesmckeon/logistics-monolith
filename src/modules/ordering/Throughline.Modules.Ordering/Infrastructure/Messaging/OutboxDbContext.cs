using Microsoft.EntityFrameworkCore;

namespace Throughline.Modules.Ordering.Infrastructure.Messaging;

internal sealed class OutboxDbContext : DbContext
{
    // Public ctor is required by AddDbContext (EF resolves the context through DI); the type
    // itself stays internal, so the module boundary is unaffected.
    public OutboxDbContext(
        DbContextOptions<OutboxDbContext> options) : base(options)
    {
    }

    public DbSet<OutboxMessage> Messages => Set<OutboxMessage>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("orders");
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}