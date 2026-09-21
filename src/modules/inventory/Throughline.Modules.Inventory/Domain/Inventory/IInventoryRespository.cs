using Throughline.Modules.Inventory.Domain.Skus;

namespace Throughline.Modules.Inventory.Domain.Inventory;

internal interface IInventoryRespository
{
    Task<IReadOnlyCollection<SkuReceipt>> GetAvailableInventoryBySkuAsync(Sku sku, CancellationToken token);
}