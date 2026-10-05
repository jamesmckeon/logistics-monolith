using Throughline.Common.Models;
using Throughline.Modules.Receiving.Application.Common;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.Inventory;
using Throughline.Modules.Receiving.Domain.Locations;
using Throughline.Modules.Receiving.Domain.Skus;

namespace Throughline.Modules.Receiving.Tests.Domain.Inventory;

[Category("Unit")]
public sealed class PalletServiceTests
{
    private const int OwnerId = 7;

    private static readonly ReceivingLocation HoldLocation = new(new UpperCaseString("HOLD-01"), LocationTypes.Hold);

    private static readonly ReceivingLocation ExceptionLocation =
        new(new UpperCaseString("EXC-01"), LocationTypes.ReceivingException);

    private static readonly ReceivingLocation AvailableLocation =
        new(new UpperCaseString("BULK-01"), LocationTypes.Bulk);

    // The location a pallet is submitted at; known, but not one of the defaults
    private static readonly ReceivingLocation SubmittedLocation =
        new(new UpperCaseString("BULK-02"), LocationTypes.Bulk);

    private static readonly DefaultLocations Defaults = new(HoldLocation, ExceptionLocation, AvailableLocation);

    private static readonly OwnerSku UntrackedSku =
        new(OwnerId, new UpperCaseString("SKU-PLAIN"), Guid.CreateVersion7(), false, false);

    private static readonly OwnerSku LotTrackedSku =
        new(OwnerId, new UpperCaseString("SKU-LOT"), Guid.CreateVersion7(), true, false);

    private static readonly OwnerSku ExpirationTrackedSku =
        new(OwnerId, new UpperCaseString("SKU-EXP"), Guid.CreateVersion7(), false, true);

    private static readonly HoldReason ActiveHoldReason = new(new UpperCaseString("DMG"), true);
    private static readonly HoldReason InactiveHoldReason = new(new UpperCaseString("OLD"), false);

    private static readonly DateTime ExpiresUtc = new(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // A pallet with nothing wrong with it; tests vary one thing at a time with `with`
    private static readonly SubmittedPallet ValidPallet = new(
        "LPN-1", "SKU-PLAIN", 10, "BULK-02", "LOT-1", ExpiresUtc, null);

    // PalletService is stateless, so one instance across tests can't leak state between them
    private readonly PalletService _sut = new();

    private static PalletServiceRequest GivenRequest(params SubmittedPallet[] pallets) =>
        new(
            OwnerId,
            pallets,
            [UntrackedSku, LotTrackedSku, ExpirationTrackedSku],
            [ActiveHoldReason, InactiveHoldReason],
            Defaults,
            [],
            [HoldLocation, ExceptionLocation, AvailableLocation, SubmittedLocation]);

    private static void AssertSingleInvalidPallet(
        PalletServiceResult result, params ReceivingExceptions[] expectedExceptions)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.ValidPallets, Is.Empty);
            Assert.That(result.InvalidPallets, Has.Count.EqualTo(1));
            Assert.That(result.InvalidPallets.Single().Exceptions, Is.EquivalentTo(expectedExceptions));
        });
    }

    #region BuildPallets

    [Test]
    public void BuildPallets_ValidPalletWithoutHold_PlacesPalletInAvailableLocation()
    {
        var request = GivenRequest(ValidPallet);

        var result = _sut.BuildPallets(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.InvalidPallets, Is.Empty);
            Assert.That(result.ValidPallets, Has.Count.EqualTo(1));
            Assert.That(result.ValidPallets.Single().Location, Is.SameAs(AvailableLocation));
            Assert.That(result.ValidPallets.Single().HoldReason, Is.Null);
        });
    }

    [Test]
    public void BuildPallets_ValidPallet_CarriesSubmittedDetails()
    {
        var request = GivenRequest(ValidPallet);

        var pallet = _sut.BuildPallets(request).ValidPallets.Single();

        Assert.Multiple(() =>
        {
            Assert.That(pallet.LicensePlateNumber.Value, Is.EqualTo("LPN-1"));
            Assert.That(pallet.OwnerSku, Is.SameAs(UntrackedSku));
            Assert.That(pallet.Quantity, Is.EqualTo(10));
            Assert.That(pallet.LotNumber, Is.EqualTo("LOT-1"));
            Assert.That(pallet.ExpiresOn?.Value, Is.EqualTo(new DateTimeOffset(ExpiresUtc)));
        });
    }

    [Test]
    public void BuildPallets_ExpiryWithoutTimeZone_ReceivesExpiryAsUtc()
    {
        var unspecifiedExpiry = DateTime.SpecifyKind(ExpiresUtc, DateTimeKind.Unspecified);
        var request = GivenRequest(ValidPallet with { Expires = unspecifiedExpiry });

        var pallet = _sut.BuildPallets(request).ValidPallets.Single();

        Assert.That(pallet.ExpiresOn?.Value, Is.EqualTo(new DateTimeOffset(ExpiresUtc)));
    }

    [Test]
    public void BuildPallets_ValidPalletWithActiveHold_PlacesPalletInHoldLocation()
    {
        var request = GivenRequest(ValidPallet with { HoldReasonCode = "DMG" });

        var result = _sut.BuildPallets(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.InvalidPallets, Is.Empty);
            Assert.That(result.ValidPallets, Has.Count.EqualTo(1));
            Assert.That(result.ValidPallets.Single().Location, Is.SameAs(HoldLocation));
            Assert.That(result.ValidPallets.Single().HoldReason, Is.SameAs(ActiveHoldReason));
        });
    }

    [Test]
    public void BuildPallets_UnknownSku_ReturnsInvalidPalletWithInvalidSkuCode()
    {
        var request = GivenRequest(ValidPallet with { Sku = "SKU-UNKNOWN" });

        var result = _sut.BuildPallets(request);

        AssertSingleInvalidPallet(result, ReceivingExceptions.InvalidSkuCode);
    }

    [Test]
    public void BuildPallets_LotTrackedSkuWithoutLot_ReturnsInvalidPalletWithLotNumberRequired()
    {
        var request = GivenRequest(ValidPallet with { Sku = "SKU-LOT", LotNumber = null });

        var result = _sut.BuildPallets(request);

        AssertSingleInvalidPallet(result, ReceivingExceptions.LotNumberRequired);
    }

    [Test]
    public void BuildPallets_ExpirationTrackedSkuWithoutExpiry_ReturnsInvalidPalletWithExpiryRequired()
    {
        var request = GivenRequest(ValidPallet with { Sku = "SKU-EXP", Expires = null });

        var result = _sut.BuildPallets(request);

        AssertSingleInvalidPallet(result, ReceivingExceptions.ExpirationDateRequired);
    }

    [Test]
    public void BuildPallets_LpnAlreadyReceived_ReturnsInvalidPalletWithDuplicateLpn()
    {
        var request = GivenRequest(ValidPallet) with
        {
            ExistingLpns = [new ReceivedLpn(new UpperCaseString("LPN-1"), AppDateTime.Now)]
        };

        var result = _sut.BuildPallets(request);

        AssertSingleInvalidPallet(result, ReceivingExceptions.DuplicateLpn);
    }

    [Test]
    public void BuildPallets_UnknownLocation_ReturnsInvalidPalletWithInvalidLocation()
    {
        var request = GivenRequest(ValidPallet with { LocationId = "NOWHERE" });

        var result = _sut.BuildPallets(request);

        AssertSingleInvalidPallet(result, ReceivingExceptions.InvalidLocation);
    }

    [Test]
    public void BuildPallets_UnknownHoldReason_ReturnsInvalidPalletWithInvalidHoldReason()
    {
        var request = GivenRequest(ValidPallet with { HoldReasonCode = "UNKNOWN" });

        var result = _sut.BuildPallets(request);

        AssertSingleInvalidPallet(result, ReceivingExceptions.InvalidHoldReason);
    }

    [Test]
    public void BuildPallets_InactiveHoldReason_ReturnsInvalidPalletWithInvalidHoldReason()
    {
        var request = GivenRequest(ValidPallet with { HoldReasonCode = "OLD" });

        var result = _sut.BuildPallets(request);

        AssertSingleInvalidPallet(result, ReceivingExceptions.InvalidHoldReason);
    }

    [Test]
    public void BuildPallets_PalletWithSeveralProblems_RecordsEveryException()
    {
        var request = GivenRequest(ValidPallet with { LocationId = "NOWHERE", HoldReasonCode = "UNKNOWN" }) with
        {
            ExistingLpns = [new ReceivedLpn(new UpperCaseString("LPN-1"), AppDateTime.Now)]
        };

        var result = _sut.BuildPallets(request);

        AssertSingleInvalidPallet(
            result,
            ReceivingExceptions.DuplicateLpn,
            ReceivingExceptions.InvalidLocation,
            ReceivingExceptions.InvalidHoldReason);
    }

    [Test]
    public void BuildPallets_InvalidPallet_PlacesItInExceptionLocationWithDetails()
    {
        var request = GivenRequest(ValidPallet with { Sku = "SKU-UNKNOWN" });

        var invalidPallet = _sut.BuildPallets(request).InvalidPallets.Single();

        Assert.Multiple(() =>
        {
            Assert.That(invalidPallet.Location, Is.SameAs(ExceptionLocation));
            Assert.That(invalidPallet.RequestLocationId, Is.EqualTo("BULK-02"));
            Assert.That(invalidPallet.OwnerId, Is.EqualTo(OwnerId));
            Assert.That(invalidPallet.SkuCode.Value, Is.EqualTo("SKU-UNKNOWN"));
            Assert.That(invalidPallet.LicensePlateNumber.Value, Is.EqualTo("LPN-1"));
            Assert.That(invalidPallet.Quantity, Is.EqualTo(10));
            Assert.That(invalidPallet.LotNumber, Is.EqualTo("LOT-1"));
            Assert.That(invalidPallet.ExpiresOn?.Value, Is.EqualTo(new DateTimeOffset(ExpiresUtc)));
        });
    }

    [Test]
    public void BuildPallets_InvalidPalletExpiryWithoutTimeZone_KeepsExpiryAsUtc()
    {
        var unspecifiedExpiry = DateTime.SpecifyKind(ExpiresUtc, DateTimeKind.Unspecified);
        var request = GivenRequest(ValidPallet with { Sku = "SKU-UNKNOWN", Expires = unspecifiedExpiry });

        var invalidPallet = _sut.BuildPallets(request).InvalidPallets.Single();

        Assert.That(invalidPallet.ExpiresOn?.Value, Is.EqualTo(new DateTimeOffset(ExpiresUtc)));
    }

    [Test]
    public void BuildPallets_ValidAndInvalidPallets_ReturnsEachPalletOnce()
    {
        var request = GivenRequest(
            ValidPallet,
            ValidPallet with { Lpn = "LPN-2", Sku = "SKU-UNKNOWN" });

        var result = _sut.BuildPallets(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.ValidPallets.Select(p => p.LicensePlateNumber.Value), Is.EqualTo(new[] { "LPN-1" }));
            Assert.That(result.InvalidPallets.Select(p => p.LicensePlateNumber.Value),
                Is.EqualTo(new[] { "LPN-2" }));
        });
    }

    #endregion
}