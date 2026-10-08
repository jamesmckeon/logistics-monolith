using Throughline.Modules.Receiving.Domain.Locations;

namespace Throughline.Modules.Receiving.Application.Configuration;

internal sealed record DefaultLocationSetting
{
    public required LocationTypes LocationType { get; init; }
    public required string LocationId { get; init; }
}
