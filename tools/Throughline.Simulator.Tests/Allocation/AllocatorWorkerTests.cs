using System.Globalization;
using Throughline.Simulator.Allocation;
using Throughline.Simulator.Configuration;

namespace Throughline.Simulator.Tests.Allocation;

[Category("Unit")]
public sealed class AllocatorWorkerTests
{
    private const string DayAllocator = "allocator1@example.com";
    private const string EveningAllocator = "allocator2@example.com";

    private static readonly TimeZoneInfo Chicago = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    private static readonly DayOfWeek[] Weekdays =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

    // weekdays 06:00-14:30 and 14:30-23:00, Saturday 06:00-14:30
    private static readonly List<ShiftOptions> DockShifts =
    [
        new() { Days = Weekdays, Start = TimeSpan.FromHours(6), End = TimeSpan.FromHours(14.5) },
        new() { Days = Weekdays, Start = TimeSpan.FromHours(14.5), End = TimeSpan.FromHours(23) },
        new() { Days = [DayOfWeek.Saturday], Start = TimeSpan.FromHours(6), End = TimeSpan.FromHours(14.5) }
    ];

    private static readonly WorklistOrder First = Order("019a0000-0000-7000-8000-000000000001");
    private static readonly WorklistOrder Second = Order("019a0000-0000-7000-8000-000000000002");
    private static readonly WorklistOrder Third = Order("019a0000-0000-7000-8000-000000000003");

    #region OnShift

    // Chicago is UTC-5 until 2026-11-01: 2026-10-19 is a Monday
    [TestCase("2026-10-19T15:00:00Z", DayAllocator, TestName = "OnShift_MondayTenAm_DayAllocator")]
    [TestCase("2026-10-19T19:30:00Z", EveningAllocator, TestName = "OnShift_MondayShiftChangeAt1430_EveningAllocator")]
    [TestCase("2026-10-20T03:00:00Z", EveningAllocator, TestName = "OnShift_MondayTenPm_EveningAllocator")]
    [TestCase("2026-10-24T15:00:00Z", DayAllocator, TestName = "OnShift_SaturdayTenAm_DayAllocator")]
    public void OnShift_WhileFacilityIsOpen_ThatShiftsAllocator(string instant, string expected)
    {
        var facility = Facility(
            Allocator(DayAllocator, 0, 2),
            Allocator(EveningAllocator, 1));

        var actual = AllocatorWorker.OnShift(facility, Chicago, Parse(instant));

        Assert.That(actual?.Username, Is.EqualTo(expected));
    }

    [TestCase("2026-10-25T15:00:00Z", TestName = "OnShift_SundayTenAm_Null")]
    [TestCase("2026-10-20T04:30:00Z", TestName = "OnShift_MondayElevenThirtyPm_Null")]
    public void OnShift_FacilityClosed_Null(string instant)
    {
        var facility = Facility(
            Allocator(DayAllocator, 0, 2),
            Allocator(EveningAllocator, 1));

        var actual = AllocatorWorker.OnShift(facility, Chicago, Parse(instant));

        Assert.That(actual, Is.Null);
    }

    [Test]
    public void OnShift_ShiftWithoutAllocator_Null()
    {
        var facility = Facility(Allocator(DayAllocator, 0, 2));

        var actual = AllocatorWorker.OnShift(facility, Chicago, Parse("2026-10-19T22:00:00Z"));

        Assert.That(actual, Is.Null);
    }

    #endregion

    #region SelectWave

    [Test]
    public void SelectWave_BothLists_BackorderedFirst()
    {
        var actual = AllocatorWorker.SelectWave([Third], [First, Second], 500);

        Assert.That(actual, Is.EqualTo(new[] { Third.OrderId, First.OrderId, Second.OrderId }));
    }

    [Test]
    public void SelectWave_OrderOnBothLists_ReleasedOnce()
    {
        var actual = AllocatorWorker.SelectWave([First], [First, Second], 500);

        Assert.That(actual, Is.EqualTo(new[] { First.OrderId, Second.OrderId }));
    }

    [Test]
    public void SelectWave_MoreThanMaximum_TakesEarliestUpToMaximum()
    {
        var actual = AllocatorWorker.SelectWave([First], [Second, Third], 2);

        Assert.That(actual, Is.EqualTo(new[] { First.OrderId, Second.OrderId }));
    }

    [Test]
    public void SelectWave_NothingWaiting_Empty()
    {
        var actual = AllocatorWorker.SelectWave([], [], 500);

        Assert.That(actual, Is.Empty);
    }

    #endregion

    private static FacilityOptions Facility(params AllocatorOptions[] allocators) => new()
    {
        TimeZone = "America/Chicago",
        DockShifts = DockShifts,
        Allocators = allocators.ToList()
    };

    private static AllocatorOptions Allocator(string username, params int[] dockShifts) =>
        new() { Username = username, Password = "password", DockShifts = dockShifts };

    private static WorklistOrder Order(string orderId) =>
        new(Guid.Parse(orderId), new DateTimeOffset(2026, 10, 19, 15, 0, 0, TimeSpan.Zero));

    private static DateTimeOffset Parse(string value) => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
}
