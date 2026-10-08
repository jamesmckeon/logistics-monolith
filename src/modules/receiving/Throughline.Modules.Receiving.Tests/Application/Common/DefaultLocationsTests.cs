using Throughline.Modules.Receiving.Application.Common;
using Throughline.Modules.Receiving.Application.Configuration;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.Locations;

namespace Throughline.Modules.Receiving.Tests.Application.Common;

[Category("Unit")]
public sealed class DefaultLocationsTests
{
    private static readonly ReceivingLocation HoldLocation = new(new UpperCaseString("HOLD-01"), LocationTypes.Hold);

    private static readonly ReceivingLocation ExceptionLocation =
        new(new UpperCaseString("EXC-01"), LocationTypes.ReceivingException);

    private static readonly ReceivingLocation BulkLocation = new(new UpperCaseString("BULK-01"), LocationTypes.Bulk);

    private static readonly ReceivingLocation[] AllLocations = [HoldLocation, ExceptionLocation, BulkLocation];

    private static AppConfiguration GivenConfiguration(
        string holdLocationId, string exceptionLocationId, string bulkLocationId) =>
        new()
        {
            DefaultLocations =
            [
                new DefaultLocationSetting { LocationType = LocationTypes.Hold, LocationId = holdLocationId },
                new DefaultLocationSetting
                    { LocationType = LocationTypes.ReceivingException, LocationId = exceptionLocationId },
                new DefaultLocationSetting { LocationType = LocationTypes.Bulk, LocationId = bulkLocationId }
            ]
        };

    #region Resolve

    [Test]
    public void Resolve_ConfiguredLocationsExist_ReturnsLocationForEachRole()
    {
        var configuration = GivenConfiguration("HOLD-01", "EXC-01", "BULK-01");

        var defaultLocations = DefaultLocations.Resolve(configuration, AllLocations);

        Assert.Multiple(() =>
        {
            Assert.That(defaultLocations.HoldLocation, Is.SameAs(HoldLocation));
            Assert.That(defaultLocations.ExceptionLocation, Is.SameAs(ExceptionLocation));
            Assert.That(defaultLocations.AvailableLocation, Is.SameAs(BulkLocation));
        });
    }

    [TestCase(nameof(LocationTypes.Hold))]
    [TestCase(nameof(LocationTypes.ReceivingException))]
    [TestCase(nameof(LocationTypes.Bulk))]
    public void Resolve_ConfiguredLocationMissing_ThrowsInvalidOperationException(string missingLocationType)
    {
        var configuration = GivenConfiguration("HOLD-01", "EXC-01", "BULK-01");
        var missingType = Enum.Parse<LocationTypes>(missingLocationType);
        var locations = AllLocations.Where(l => l.LocationType != missingType).ToArray();

        Assert.That(() => DefaultLocations.Resolve(configuration, locations), Throws.InvalidOperationException);
    }

    [Test]
    public void Resolve_ConfiguredLocationOfWrongType_ThrowsInvalidOperationException()
    {
        var configuration = GivenConfiguration("BULK-01", "EXC-01", "BULK-01");

        Assert.That(() => DefaultLocations.Resolve(configuration, AllLocations), Throws.InvalidOperationException);
    }

    #endregion
}
