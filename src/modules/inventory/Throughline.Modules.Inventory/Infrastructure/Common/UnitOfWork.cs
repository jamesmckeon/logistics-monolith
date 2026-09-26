using Microsoft.EntityFrameworkCore.Storage;
using Throughline.Modules.Inventory.Infrastructure.Db;

namespace Throughline.Modules.Inventory.Infrastructure.Common;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly InventoryDbContext _dbContext;

    public UnitOfWork(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveChangesAsync(CancellationToken token)
    {
        await _dbContext.SaveChangesAsync(token);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token)
    {
        return _dbContext.Database.BeginTransactionAsync(token);
    }
}