using Throughline.Simulator.Buying;
using Throughline.Simulator.Configuration;
using Throughline.Simulator.Ledger;
using Throughline.Simulator.Scheduling;

namespace Throughline.Simulator.Tests.Buying;

[Category("Unit")]
public sealed class ShipmentPlannerTests
{
    private static readonly TimeZoneInfo Chicago = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    private static readonly DayOfWeek[] EveryDay = Enum.GetValues<DayOfWeek>();

    private static readonly FacilityOptions Facility = new()
    {
        TimeZone = "America/Chicago",
        DockShifts = [new ShiftOptions { Days = EveryDay, Start = TimeSpan.FromHours(6), End = TimeSpan.FromHours(23) }],
        PutawayLocationPrefix = "BULK-",
        PutawayLocationCount = 40,
        CarrierScacs = ["ABFS", "ODFL"]
    };

    private static readonly BuyingProfile Buying = new()
    {
        SupplierName = "Ridgeline Apparel Manufacturing",
        UnitsPerPallet = 48,
        PalletsPerTrailer = 26,
        LeadTimeDaysMin = 5,
        LeadTimeDaysMax = 10,
        LateShipmentRate = 0
    };

    // Monday 2026-10-19, 09:00 Chicago
    private static readonly DateTimeOffset OrderedAt = new(2026, 10, 19, 14, 0, 0, TimeSpan.Zero);

    // 30 SKUs of 100 units: three pallets each (48, 48, 4), so 90 pallets on four trailers
    private static readonly IReadOnlyList<PurchaseLine> Lines =
        Enumerable.Range(1, 30).Select(n => new PurchaseLine($"HFO-{n:D6}", 100)).ToList();

    private ShipmentPlanner _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new ShipmentPlanner(Buying, Facility, new DockCalendar(Chicago, Facility.DockShifts), new Random(21));
    }

    #region Plan

    [Test]
    public void Plan_ThirtyLines_ShipsEveryUnitOnSingleSkuPallets()
    {
        var actual = _sut.Plan(1, "HFO", PurchaseOrderKind.Weekly, Lines, OrderedAt);

        var pallets = actual.Shipments.SelectMany(s => s.Pallets).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(pallets.GroupBy(p => p.Sku).Select(g => g.Sum(p => p.Quantity)), Is.All.EqualTo(100));
            Assert.That(pallets.Select(p => p.Quantity), Is.All.InRange(1, 48));
            Assert.That(pallets.Select(p => p.Lpn), Is.Unique);
        });
    }

    [Test]
    public void Plan_NinetyPallets_FourTrailersOfAtMostTwentySix()
    {
        var actual = _sut.Plan(1, "HFO", PurchaseOrderKind.Weekly, Lines, OrderedAt);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Shipments, Has.Count.EqualTo(4));
            Assert.That(actual.Shipments.Select(s => s.Pallets.Count), Is.All.LessThanOrEqualTo(26));
        });
    }

    [Test]
    public void Plan_WeeklyOrder_TrailersDueWithinLeadTime()
    {
        var actual = _sut.Plan(1, "HFO", PurchaseOrderKind.Weekly, Lines, OrderedAt);

        var dueDates = actual.Shipments.Select(s => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(s.DueAt, Chicago).DateTime));
        Assert.That(dueDates, Is.All.InRange(new DateOnly(2026, 10, 24), new DateOnly(2026, 10, 29)));
    }

    [Test]
    public void Plan_Order_DeliveryDetailsFromFacilityAndSupplier()
    {
        var actual = _sut.Plan(1, "HFO", PurchaseOrderKind.Weekly, Lines, OrderedAt);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Shipments.Select(s => s.CarrierScac), Is.All.AnyOf("ABFS", "ODFL"));
            Assert.That(actual.Shipments.Select(s => s.ShipperName), Is.All.EqualTo("Ridgeline Apparel Manufacturing"));
            Assert.That(actual.Shipments.Select(s => s.OwnerId), Is.All.EqualTo(1));
            Assert.That(actual.Shipments.SelectMany(s => s.Pallets).Select(p => p.LocationId),
                Is.All.Matches<string>(l => l.StartsWith("BULK-", StringComparison.Ordinal)));
        });
    }

    #endregion
}
