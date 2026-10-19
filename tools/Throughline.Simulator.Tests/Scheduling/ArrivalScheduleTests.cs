using Throughline.Simulator.Scheduling;

namespace Throughline.Simulator.Tests.Scheduling;

[Category("Unit")]
public sealed class ArrivalScheduleTests
{
    private static readonly TimeZoneInfo Chicago = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
    private static readonly double[] FlatWeekdays = [1, 1, 1, 1, 1, 1, 1];

    [Test]
    public void RatePerHour_MondayPeakHour_ScalesByHourShareAndWeekdayFactor()
    {
        // hour 20 holds 25 of 48 weight; Monday is twice the other days, so 1.75x the average day
        var hourly = Enumerable.Repeat(1.0, 24).ToArray();
        hourly[20] = 25;
        double[] weekdays = [1, 2, 1, 1, 1, 1, 1];
        var sut = new ArrivalSchedule(Chicago, 240, hourly, weekdays);
        var mondayEightThirtyPmCentral = new DateTimeOffset(2026, 10, 20, 1, 30, 0, TimeSpan.Zero);

        var actual = sut.RatePerHour(mondayEightThirtyPmCentral);

        Assert.That(actual, Is.EqualTo(240 * 1.75 * 25 / 48).Within(1e-9));
    }

    // daylight saving ends 2026-11-01: 12:00Z is 07:00 CDT the day before, 06:00 CST that day
    [TestCase("2026-10-31T12:00:00Z", 0.0)]
    [TestCase("2026-11-01T12:00:00Z", 24.0)]
    public void RatePerHour_AroundDaylightSavingChange_UsesLocalHour(string instant, double expected)
    {
        var sut = new ArrivalSchedule(Chicago, 24, OnlyHour(6), FlatWeekdays);

        var actual = sut.RatePerHour(DateTimeOffset.Parse(instant, System.Globalization.CultureInfo.InvariantCulture));

        Assert.That(actual, Is.EqualTo(expected).Within(1e-9));
    }

    #region NextDelay

    [Test]
    public void NextDelay_ConstantRate_GapsAverageDayDividedByOrders()
    {
        // 96 orders a day spread evenly is one every 15 minutes on average
        var sut = new ArrivalSchedule(Chicago, 96, Enumerable.Repeat(1.0, 24).ToArray(), FlatWeekdays);
        var random = new Random(42);
        var now = new DateTimeOffset(2026, 10, 19, 15, 0, 0, TimeSpan.Zero);

        var meanMinutes = Enumerable.Range(0, 5000)
            .Select(_ => sut.NextDelay(now, random).TotalMinutes)
            .Average();

        Assert.That(meanMinutes, Is.EqualTo(15).Within(3).Percent);
    }

    [Test]
    public void NextDelay_OnlyOneHourWeighted_ArrivesInThatLocalHour()
    {
        var sut = new ArrivalSchedule(Chicago, 24, OnlyHour(9), FlatWeekdays);
        var mondayEightAmCentral = new DateTimeOffset(2026, 10, 19, 13, 0, 0, TimeSpan.Zero);

        var delay = sut.NextDelay(mondayEightAmCentral, new Random(7));

        var arrival = TimeZoneInfo.ConvertTime(mondayEightAmCentral + delay, Chicago);
        Assert.That(arrival.Hour, Is.EqualTo(9));
    }

    [Test]
    public void NextDelay_AllHourlyWeightsZero_ThrowsInvalidOperation()
    {
        var sut = new ArrivalSchedule(Chicago, 24, new double[24], FlatWeekdays);

        Assert.Throws<InvalidOperationException>(() =>
            sut.NextDelay(new DateTimeOffset(2026, 10, 19, 13, 0, 0, TimeSpan.Zero), new Random(1)));
    }

    #endregion

    private static double[] OnlyHour(int hour)
    {
        var weights = new double[24];
        weights[hour] = 1;
        return weights;
    }
}
