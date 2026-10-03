using Throughline.Common.Models;
using Throughline.Modules.Receiving.Domain.Common;

namespace Throughline.Modules.Receiving.Domain.Locations;

internal sealed class ReceivingLocation : Entity<UpperCaseString>
{
    public ReceivingLocation(UpperCaseString locationId, LocationTypes locationType) : base(locationId)
    {
        LocationType = locationType;
    }

    public LocationTypes LocationType { get; }

    public bool IsHoldLocation => LocationType is LocationTypes.Bulk;
    public bool IsExceptionLocation => LocationType == LocationTypes.ReceivingException;
}