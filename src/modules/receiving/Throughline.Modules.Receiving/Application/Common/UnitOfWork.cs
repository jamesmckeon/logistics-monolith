using Throughline.Modules.Receiving.Infrastructure.Db;

namespace Throughline.Modules.Receiving.Application.Common;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly ReceivingDbContext _dbContext;

    public UnitOfWork(ReceivingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveChangesAsync(CancellationToken token)
    {
        await _dbContext.SaveChangesAsync(token);
    }
}