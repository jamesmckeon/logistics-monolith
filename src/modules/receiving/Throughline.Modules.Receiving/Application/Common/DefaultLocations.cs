using Throughline.Modules.Receiving.Application.Configuration;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.Locations;

namespace Throughline.Modules.Receiving.Application.Common;

internal sealed record DefaultLocations(
    ReceivingLocation HoldLocation,
    ReceivingLocation ExceptionLocation,
    ReceivingLocation AvailableLocation)
{
    /// <summary>
    ///     Resolves the configured default location ids to their receiving locations; pallets need the location
    ///     records themselves, both for their location type rules and as foreign keys
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     A configured location doesn't exist in <paramref name="locations" />, or isn't of the type it's the
    ///     default for
    /// </exception>
    public static DefaultLocations Resolve(AppConfiguration configuration, IEnumerable<ReceivingLocation> locations)
    {
        var settings = configuration.DefaultLocations.ToArray();
        var locationArray = locations.ToArray();

        ReceivingLocation ResolveLocation(LocationTypes locationType)
        {
            var locationId = new UpperCaseString(settings.Single(s => s.LocationType == locationType).LocationId);

            var location = locationArray.SingleOrDefault(l => l.Id == locationId)
                           ?? throw new InvalidOperationException(
                               $"The default {locationType} location '{locationId.Value}' doesn't exist");

            if (location.LocationType != locationType)
            {
                throw new InvalidOperationException(
                    $"The default {locationType} location '{locationId.Value}' is a {location.LocationType} location");
            }

            return location;
        }

        return new DefaultLocations(
            ResolveLocation(LocationTypes.Hold),
            ResolveLocation(LocationTypes.ReceivingException),
            ResolveLocation(LocationTypes.Bulk));
    }
}
