using Microsoft.EntityFrameworkCore;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Domain.Skus;
using Throughline.Modules.Inventory.Infrastructure.Common;

namespace Throughline.Modules.Inventory.Infrastructure.Inventory;

internal sealed class InventoryDbContext : DbContext, IInventoryRepository
{
    private readonly ILogger<InventoryDbContext> _logger;

    public InventoryDbContext(
        DbContextOptions<InventoryDbContext> options,
        ILogger<InventoryDbContext> logger) : base(options)
    {
        _logger = logger;
    }

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

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<EntityId>().HaveConversion<EntityIdValueConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("inventory");

        // Scope to this context's own configurations — the assembly also holds the
        // OrderAllocation context's configs, which must not be pulled into this model.
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(InventoryDbContext).Assembly,
            t => t.Namespace == typeof(InventoryDbContext).Namespace);
    }
}