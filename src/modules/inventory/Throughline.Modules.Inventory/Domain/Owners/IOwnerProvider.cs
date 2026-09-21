namespace Throughline.Modules.Inventory.Domain.Owners;

internal interface IOwnerProvider
{
    Task<Owner?> GetOwnerByIdAsync(int ownerId);
}