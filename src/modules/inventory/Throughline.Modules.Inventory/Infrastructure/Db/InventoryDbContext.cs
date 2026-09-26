using Microsoft.EntityFrameworkCore;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Domain.Owners;
using Throughline.Modules.Inventory.Domain.Skus;
using Throughline.Modules.Inventory.Infrastructure.Common;

namespace Throughline.Modules.Inventory.Infrastructure.Db;

public sealed class InventoryDbContext :
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

    async Task<IReadOnlyCollection<SkuReceipt>> IInventoryRepository.GetAvailableInventoryAsync(
        IEnumerable<EntityId> skuIds, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(skuIds);

        var ids = skuIds.ToArray();
        if (!ids.Any())
            return Array.Empty<SkuReceipt>().ToList().AsReadOnly();

        return await SkuReceipts.Where(sr => ids.Any(a => a == sr.Id))
            .ToListAsync(token);
    }

    async Task<IReadOnlyCollection<Sku>> IInventoryRepository.GetSkusByOwnerIdAsync(
        int ownerId, IEnumerable<string> skuCodes, CancellationToken token)
    {
        var codes = skuCodes.ToArray();
        if (!codes.Any())
            return Array.Empty<Sku>().ToList().AsReadOnly();

        return await Skus.Where(s => s.OwnerId == ownerId && codes.Any(a => a == s.Code))
            .ToListAsync(token);
    }

    async Task<IReadOnlyCollection<Sku>> IInventoryRepository.GetSkusByIdAsync(IEnumerable<EntityId> skuIds, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(skuIds);

        var skuIdsArray = skuIds.ToArray();

        if (!skuIdsArray.Any())
            return [];

        return await Skus.Where(s => skuIdsArray.Contains(s.Id))
            .ToListAsync(token);
    }

    void IOrderAllocationRepository.Add(OrderAllocation order)
    {
        ArgumentNullException.ThrowIfNull(order);

        Orders.Add(order);
    }

    async Task<OrderAllocation?> IOrderAllocationRepository.GetByOrderIdAsync(Guid orderId, CancellationToken token)
    {
        return await Orders.FirstOrDefaultAsync(o => o.Id == orderId, token);
    }

    async Task<IReadOnlyCollection<OrderAllocation>> IOrderAllocationRepository.GetAllByOrderIdAsync(IEnumerable<Guid> orderIds)
    {
        ArgumentNullException.ThrowIfNull(orderIds);

        var orderIdsArray = orderIds.ToArray();

        if (!orderIdsArray.Any())
            return [];

        var orders = await Orders.Where(o => orderIdsArray.Any(a => a == o.Id)).ToListAsync();
        return orders.AsReadOnly();
    }

    async Task<Owner?> IOwnerProvider.GetOwnerByIdAsync(int ownerId, CancellationToken token)
    {
        return await Owners.SingleOrDefaultAsync(o => o.Id == ownerId, token);
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