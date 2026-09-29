using Microsoft.EntityFrameworkCore.Storage;

namespace Throughline.Modules.Inventory.Infrastructure.Common;

public interface IUnitOfWork
{
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token);
    Task SaveChangesAsync(CancellationToken token);
    void ClearChanges();
}