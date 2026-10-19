using Throughline.Simulator.Configuration;
using Throughline.Simulator.Scheduling;

namespace Throughline.Simulator.Tests.Scheduling;

[Category("Unit")]
public sealed class DockCalendarTests
{
    private static readonly TimeZoneInfo Chicago = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    private static readonly DayOfWeek[] Weekdays =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

    // weekdays 06:00-14:30 and 14:30-23:00, Saturday 06:00-14:30
    private static readonly List<ShiftOptions> Shifts =
    [
        new() { Days = Weekdays, Start = TimeSpan.FromHours(6), End = TimeSpan.FromHours(14.5) },
        new() { Days = Weekdays, Start = TimeSpan.FromHours(14.5), End = TimeSpan.FromHours(23) },
        new() { Days = [DayOfWeek.Saturday], Start = TimeSpan.FromHours(6), End = TimeSpan.FromHours(14.5) }
    ];

    private DockCalendar _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new DockCalendar(Chicago, Shifts);
    }

    // times are Chicago daylight time (UTC-5): 2026-10-19 is a Monday
    [TestCase("2026-10-19T15:00:00Z", true, TestName = "IsOpen_MondayTenAm_True")]
    [TestCase("2026-10-19T19:30:00Z", true, TestName = "IsOpen_MondayShiftChangeAt1430_True")]
    [TestCase("2026-10-20T04:30:00Z", false, TestName = "IsOpen_MondayElevenThirtyPm_False")]
    [TestCase("2026-10-24T20:00:00Z", false, TestName = "IsOpen_SaturdayThreePm_False")]
    [TestCase("2026-10-25T15:00:00Z", false, TestName = "IsOpen_SundayTenAm_False")]
    public void IsOpen_InstantInChicago_MatchesShifts(string instant, bool expected)
    {
        var actual = _sut.IsOpen(DateTimeOffset.Parse(instant, System.Globalization.CultureInfo.InvariantCulture));

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ArrivalOn_Sunday_ArrivesOnMondayWhileOpen()
    {
        var actual = _sut.ArrivalOn(new DateOnly(2026, 10, 25), new Random(3));

        var local = TimeZoneInfo.ConvertTime(actual, Chicago);
        Assert.Multiple(() =>
        {
            Assert.That(local.DayOfWeek, Is.EqualTo(DayOfWeek.Monday));
            Assert.That(_sut.IsOpen(actual), Is.True);
        });
    }

    [Test]
    public void ArrivalOn_ManyWeekdayArrivals_AllWhileDockIsOpen()
    {
        var random = new Random(9);

        var arrivals = Enumerable.Range(0, 2000).Select(_ => _sut.ArrivalOn(new DateOnly(2026, 10, 21), random));

        Assert.That(arrivals.Select(_sut.IsOpen), Is.All.True);
    }
}
