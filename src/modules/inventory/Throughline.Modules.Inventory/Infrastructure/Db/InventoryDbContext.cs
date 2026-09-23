using Microsoft.EntityFrameworkCore;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Domain.Owners;
using Throughline.Modules.Inventory.Domain.Skus;
using Throughline.Modules.Inventory.Infrastructure.Common;

namespace Throughline.Modules.Inventory.Infrastructure.Db;

internal sealed class InventoryDbContext :
    DbContext, IOrderAllocationRepository, IInventoryRepository, IOwnerProvider
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
    internal DbSet<Owner> Owners => Set<Owner>();

    public async Task<IReadOnlyCollection<SkuReceipt>> GetAvailableInventoryAsync(
        IEnumerable<EntityId> skuIds, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(skuIds);

        var ids = skuIds.ToArray();
        if (!ids.Any())
            return Array.Empty<SkuReceipt>().ToList().AsReadOnly();

        return await SkuReceipts.Where(sr => ids.Any(a => a == sr.Id))
            .ToListAsync(token);
    }

    public async Task<IReadOnlyCollection<Sku>> GetSkusByOwnerIdAsync(
        int ownerId, IEnumerable<string> skuCodes, CancellationToken token)
    {
        var codes = skuCodes.ToArray();
        if (!codes.Any())
            return Array.Empty<Sku>().ToList().AsReadOnly();

        return await Skus.Where(s => s.OwnerId == ownerId && codes.Any(a => a == s.Code))
            .ToListAsync(token);
    }

    public async Task<IReadOnlyCollection<Sku>> GetSkusByIdAsync(IEnumerable<EntityId> skuIds, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(skuIds);

        var skuIdsArray = skuIds.ToArray();

        if (!skuIdsArray.Any())
            return [];

        return await Skus.Where(s => skuIdsArray.Any(a => s.Id.Value == a.Value))
            .ToListAsync(token);
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

    public async Task<Owner?> GetOwnerByIdAsync(int ownerId, CancellationToken token)
    {
        return await Owners.SingleOrDefaultAsync(o => o.Id == ownerId, token);
    }


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

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(InventoryDbContext).Assembly);
    }
}