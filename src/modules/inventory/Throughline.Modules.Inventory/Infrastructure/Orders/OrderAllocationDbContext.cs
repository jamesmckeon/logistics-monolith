using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Throughline.Modules.Inventory.Domain.Allocation;

namespace Throughline.Modules.Inventory.Infrastructure.Orders;

internal sealed class OrderAllocationDbContext : DbContext, IOrderAllocationRepository
{
    private readonly ILogger<OrderAllocationDbContext> _logger;

    public OrderAllocationDbContext(
        DbContextOptions<OrderAllocationDbContext> options,
        ILogger<OrderAllocationDbContext> logger) : base(options)
    {
        _logger = logger;
    }

    internal DbSet<OrderAllocation> Orders => Set<OrderAllocation>();

    public void Add(OrderAllocation order)
    {
        ArgumentNullException.ThrowIfNull(order);

        Orders.Add(order);
    }

    public async Task<OrderAllocation?> GetByOrderId(Guid orderId, CancellationToken token)
    {
        // OrderLines is an owned collection, so EF loads it with the aggregate automatically.
        return await Orders.FirstOrDefaultAsync(o => o.Id == orderId, token);
    }

    public async Task SaveConfirmedOrder(OrderAllocation order, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(order);

        Orders.Add(order);

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
            if (pg.ConstraintName != OrderAllocationConfiguration.PrimaryKeyName)
                throw;

            _logger.LogDebug(
                "Duplicate OrderConfirmed ignored for owner {OwnerId}, order {OrderId}",
                order.OwnerId,
                order.Id);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("inventory");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderAllocationDbContext).Assembly);
    }
}