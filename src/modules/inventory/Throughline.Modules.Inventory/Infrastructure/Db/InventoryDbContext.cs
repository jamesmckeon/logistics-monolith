using Microsoft.EntityFrameworkCore;
using Npgsql;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Domain.Skus;
using Throughline.Modules.Inventory.Infrastructure.Common;

namespace Throughline.Modules.Inventory.Infrastructure.Db;

internal sealed class InventoryDbContext : DbContext, IOrderAllocationRepository, IInventoryRepository
{
    private readonly ILogger<InventoryDbContext> _logger;

    public InventoryDbContext(
        DbContextOptions<InventoryDbContext> options,
        ILogger<InventoryDbContext> logger) : base(options)
    {
        _logger = logger;
    }

    internal DbSet<OrderAllocation> Orders => Set<OrderAllocation>();
    internal DbSet<Sku> Skus => Set<Sku>();
    internal DbSet<SkuReceipt> SkuReceipts => Set<SkuReceipt>();

    public async Task<IReadOnlyCollection<SkuReceipt>> GetAvailableInventoryBySkuIdAsync(
        EntityId skuId, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(skuId);

        return await SkuReceipts.FromSql(
                $"""
                 SELECT r.* FROM inventory.sku_receipts AS r
                 WHERE r.sku_id = {skuId.Value}
                   AND r.quantity_received > (
                       SELECT COALESCE(SUM(a.quantity_allocated), 0)
                       FROM inventory.receipt_allocations AS a
                       WHERE a.sku_receipt_id = r.sku_receipt_id)
                 ORDER BY r.received_on
                 FOR UPDATE
                 """)
            .ToListAsync(token);
    }

    public async Task<IReadOnlyCollection<Sku>> GetSkusByOwnerIdAsync(int ownerId, IEnumerable<string> skuCodes)
    {
        return await Skus
            .Where(s => s.OwnerId == ownerId && skuCodes.Any(a => a == s.Code))
            .ToListAsync();
    }

    public void Add(OrderAllocation order)
    {
        ArgumentNullException.ThrowIfNull(order);

        Orders.Add(order);
    }

    public async Task<OrderAllocation?> GetByOrderId(Guid orderId, CancellationToken token)
    {
        return await Orders.FirstOrDefaultAsync(o => o.Id == orderId, token);
    }

    public async Task<IReadOnlyCollection<OrderAllocation>> GetAllByOrderId(IEnumerable<Guid> orderIds)
    {
        ArgumentNullException.ThrowIfNull(orderIds);

        var orderIdsArray = orderIds.ToArray();

        if (!orderIdsArray.Any())
            return [];

        var orders = await Orders.Where(o => orderIdsArray.Any(a => a == o.Id)).ToListAsync();
        return orders.AsReadOnly();
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

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<EntityId>().HaveConversion<EntityIdValueConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("inventory");

        // Scope to this context's own configurations — the assembly also holds the
        // Inventory context's Sku/SkuReceipt configs, which must not be pulled into this model.
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(InventoryDbContext).Assembly,
            t => t.Namespace == typeof(InventoryDbContext).Namespace);
    }
}