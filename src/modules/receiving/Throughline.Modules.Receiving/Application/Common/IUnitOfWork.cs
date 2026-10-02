namespace Throughline.Modules.Receiving.Application.Common;

internal interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken token);
}