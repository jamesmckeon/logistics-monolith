namespace Throughline.Modules.Inventory.Infrastructure.Common;

public interface IUnitOfWork
{
    Task SaveChangesAsync();
}