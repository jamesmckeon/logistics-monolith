using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Throughline.Modules.Inventory.Orders;

namespace Throughline.Modules.Inventory.Infrastructure.Orders;

public sealed class OrdersDbContext : DbContext, IOrdersRepository
{
    private readonly ILogger<OrdersDbContext> _logger;

    public OrdersDbContext(
        DbContextOptions<OrdersDbContext> options,
        ILogger<OrdersDbContext> logger) : base(options)
    {
        _logger = logger;
    }

    // Internal: the persistence record is an implementation detail, so it isn't exposed on
    // the public context surface. Callers go through the IOrdersRepository methods below,
    // which speak the domain Order.
    internal DbSet<OrderRecord> Orders => Set<OrderRecord>();

    public async Task SaveConfirmedOrder(Order order, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(order);

        Orders.Add(order.ToRecord());

        try
        {
            await SaveChangesAsync(token);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                                           {
                                               SqlState: PostgresErrorCodes.UniqueViolation
                                           } pg)
        {
            // Any unique violation other than the composite PK is unexpected — let it surface.
            if (pg.ConstraintName != OrderRecordConfiguration.PrimaryKeyName)
                throw;

            // Duplicate delivery of an already-recorded confirmed order — an idempotent no-op.
            _logger.LogDebug(
                "Duplicate OrderConfirmed ignored for owner {OwnerId}, order {OrderId}",
                order.OwnerOrderId.OwnerId,
                order.OwnerOrderId.OrderId);
        }
    }

    public async Task<Order?> GetByOwnerOrderIdAsync(OwnerOrderId id, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(id);

        var record = await Orders
            .Include(o => o.OrderLines)
            .SingleOrDefaultAsync(o => o.OwnerId == id.OwnerId && o.OrderId == id.OrderId, token);

        return record?.ToOrder();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("inventory");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
    }
}