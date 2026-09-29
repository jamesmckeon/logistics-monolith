using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Common;

namespace Throughline.Modules.Inventory.Domain.Locations;

internal sealed class InventoryLocation : Entity<EntityId>
{
    private InventoryLocation(
        EntityId id, InventoryLocationTypes locationType, string name) : base(id)
    {
        LocationType = locationType;
        Name = name;
    }

    public InventoryLocationTypes LocationType { get; }
    public string Name { get; }
}