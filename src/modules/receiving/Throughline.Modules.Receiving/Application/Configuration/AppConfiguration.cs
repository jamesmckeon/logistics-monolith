using Throughline.Modules.Receiving.Domain.Locations;

namespace Throughline.Modules.Receiving.Application.Configuration;

internal sealed record AppConfiguration
{
    public const string SectionName = "Receiving";
    
    public static readonly IReadOnlyCollection<LocationTypes> DefaultLocationTypes =
        [LocationTypes.Hold, LocationTypes.ReceivingException, LocationTypes.Bulk];

    public required IEnumerable<DefaultLocationSetting> DefaultLocations { get; init; }

    /// <summary>
    ///     True when <see cref="DefaultLocations" /> has exactly one location id for each of
    ///     <see cref="DefaultLocationTypes" />, and nothing else. The
    ///     configuration binder doesn't enforce <c>required</c>, so this is checked at startup.
    /// </summary>
    public bool HasValidDefaultLocations()
    {
        // The binder leaves a missing section null despite the non-nullable type
        if (DefaultLocations is null)
        {
            return false;
        }

        var settings = DefaultLocations.ToArray();

        return settings.All(s => !string.IsNullOrWhiteSpace(s.LocationId)) &&
               DefaultLocationTypes.All(type => settings.Count(s => s.LocationType == type) == 1) &&
               settings.Length == DefaultLocationTypes.Count;
    }
}
