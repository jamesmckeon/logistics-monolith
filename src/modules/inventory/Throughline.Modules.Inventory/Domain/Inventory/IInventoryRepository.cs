using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Skus;

namespace Throughline.Modules.Inventory.Domain.Inventory;

internal interface IInventoryRepository
{
    Task<IReadOnlyCollection<SkuReceipt>> GetAvailableInventoryAsync(IEnumerable<EntityId> skuIds,
        CancellationToken token);

    Task<IReadOnlyCollection<Sku>> GetSkusByOwnerIdAsync(int ownerId, IEnumerable<string> skuCodes,
        CancellationToken token);

    Task<IReadOnlyCollection<Sku>> GetSkusByIdAsync(IEnumerable<EntityId> skuIds,
        CancellationToken token);
}