using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Common;

namespace Throughline.Modules.Inventory.Domain.Receiving;

internal sealed class Delivery : Entity<EntityId>
{
    private Delivery(EntityId id, OwnerDeliveryReference ownerDeliveryReference) : base(id)
    {
        OwnerDeliveryReference = ownerDeliveryReference;
    }

    public OwnerDeliveryReference OwnerDeliveryReference { get; }
}