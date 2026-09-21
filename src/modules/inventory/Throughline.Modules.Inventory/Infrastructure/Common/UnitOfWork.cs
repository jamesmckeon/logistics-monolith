using Throughline.Modules.Inventory.Infrastructure.Orders;

namespace Throughline.Modules.Inventory.Infrastructure.Common;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly OrderAllocationDbContext _dbContext;

    public UnitOfWork(OrderAllocationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}