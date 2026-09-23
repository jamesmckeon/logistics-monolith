using Microsoft.EntityFrameworkCore.Storage;

namespace Throughline.Modules.Inventory.Infrastructure.Common;

public interface IUnitOfWork
{
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token);
    Task SaveChangesAsync();
    Task<IDbContextTransaction> RollbackTransactionAsync(CancellationToken token);
}