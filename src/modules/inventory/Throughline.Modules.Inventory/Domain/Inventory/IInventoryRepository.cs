using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Locations;
using Throughline.Modules.Inventory.Domain.Receiving;
using Throughline.Modules.Inventory.Domain.Skus;

namespace Throughline.Modules.Inventory.Domain.Inventory;

internal interface IInventoryRepository
{
    Task<IReadOnlyCollection<InventoryPallet>> GetAvailableInventoryAsync(IEnumerable<EntityId> skuIds,
        CancellationToken token);

    Task<IReadOnlyCollection<Sku>> GetSkusByOwnerIdAsync(int ownerId, IEnumerable<string> skuCodes,
        CancellationToken token);

    Task<IReadOnlyCollection<Sku>> GetSkusByIdAsync(IEnumerable<EntityId> skuIds,
        CancellationToken token);

    Task<IReadOnlyCollection<InventoryLocation>> GetLocationsByTypeAsync(
        InventoryLocationTypes locationType,
        CancellationToken token);
}