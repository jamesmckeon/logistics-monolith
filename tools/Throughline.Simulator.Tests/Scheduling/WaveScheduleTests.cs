using System.Globalization;
using Throughline.Simulator.Configuration;
using Throughline.Simulator.Scheduling;

namespace Throughline.Simulator.Tests.Scheduling;

[Category("Unit")]
public sealed class WaveScheduleTests
{
    private static readonly TimeZoneInfo Chicago = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    private static readonly DayOfWeek[] Weekdays =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

    private static readonly TimeSpan[] WeekdayTimes =
    [
        TimeSpan.FromHours(6.5), TimeSpan.FromHours(9), TimeSpan.FromHours(11.5),
        TimeSpan.FromHours(13.5), TimeSpan.FromHours(17), TimeSpan.FromHours(22)
    ];

    private static readonly TimeSpan[] SaturdayTimes =
        [TimeSpan.FromHours(6.5), TimeSpan.FromHours(9), TimeSpan.FromHours(11.5), TimeSpan.FromHours(13.5)];

    // same-day cutoff 14:00; weekday and Saturday waves as in appsettings.json
    private static readonly AllocationProfile Allocation = new()
    {
        Enabled = true,
        SameDayCutoff = TimeSpan.FromHours(14),
        Waves =
        [
            new WaveTimesOptions { Days = Weekdays, Times = WeekdayTimes },
            new WaveTimesOptions { Days = [DayOfWeek.Saturday], Times = SaturdayTimes }
        ]
    };

    private WaveSchedule _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new WaveSchedule(Chicago, Allocation);
    }

    // Chicago is UTC-5 until 2026-11-01: 2026-10-19 is a Monday, 2026-10-24 a Saturday
    [TestCase("2026-10-19T10:00:00Z", "2026-10-19T11:30:00Z", TestName = "Next_MondayBeforeFirstWave_SixThirty")]
    [TestCase("2026-10-19T11:30:00Z", "2026-10-19T14:00:00Z", TestName = "Next_MondayAtSixThirty_NineAm")]
    [TestCase("2026-10-19T19:00:00Z", "2026-10-19T22:00:00Z", TestName = "Next_MondayAfterCutoff_FivePm")]
    [TestCase("2026-10-20T03:30:00Z", "2026-10-20T11:30:00Z", TestName = "Next_MondayAfterLastWave_TuesdayMorning")]
    [TestCase("2026-10-24T19:00:00Z", "2026-10-26T11:30:00Z", TestName = "Next_SaturdayAfterLastWave_MondayMorning")]
    public void Next_After_FirstWaveStrictlyAfter(string after, string expected)
    {
        var actual = _sut.Next(Parse(after));

        Assert.That(actual, Is.EqualTo(Parse(expected)));
    }

    [Test]
    public void Next_AcrossDaylightSavingEnd_StillSixThirtyLocal()
    {
        // Saturday 2026-10-31 after the last wave; Monday 2026-11-02 is on standard time (UTC-6)
        var actual = _sut.Next(Parse("2026-10-31T19:00:00Z"));

        Assert.That(actual, Is.EqualTo(Parse("2026-11-02T12:30:00Z")));
    }

    [TestCase("2026-10-19T18:30:00Z", true, TestName = "IsSameDay_OneThirtyPm_True")]
    [TestCase("2026-10-19T19:00:00Z", true, TestName = "IsSameDay_AtCutoff_True")]
    [TestCase("2026-10-19T22:00:00Z", false, TestName = "IsSameDay_FivePm_False")]
    public void IsSameDay_WaveInChicago_ComparedWithCutoff(string wave, bool expected)
    {
        var actual = _sut.IsSameDay(Parse(wave));

        Assert.That(actual, Is.EqualTo(expected));
    }

    private static DateTimeOffset Parse(string value) => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
}
