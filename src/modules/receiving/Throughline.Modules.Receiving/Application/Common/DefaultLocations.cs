using Throughline.Modules.Receiving.Domain.Locations;

namespace Throughline.Modules.Receiving.Application.Common;

internal sealed record DefaultLocations(
    ReceivingLocation HoldLocation,
    ReceivingLocation ExceptionLocation,
    ReceivingLocation BulkLocation)
{
}