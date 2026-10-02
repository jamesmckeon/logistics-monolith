using Throughline.Common.Models;

namespace Throughline.Modules.Receiving.Domain.Locations;

internal sealed class ReceivingLocation : Entity<string>
{
    public ReceivingLocation(string id, LocationTypes locationType) : base(id)
    {
        LocationType = locationType;
    }

    public LocationTypes LocationType { get; }

    public bool IsHoldLocation() => LocationType is LocationTypes.Bulk or LocationTypes.ReceivingException;
}