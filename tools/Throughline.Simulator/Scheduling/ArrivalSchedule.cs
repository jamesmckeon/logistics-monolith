using Throughline.Simulator.Configuration;

namespace Throughline.Simulator.Scheduling;

/// <summary>
///     When a customer's next order arrives. Arrivals are a Poisson process whose rate follows the
///     customer's hourly and weekday curves in their local time, so gaps vary randomly around the
///     current average instead of keeping a fixed beat.
/// </summary>
internal sealed class ArrivalSchedule
{
    // Every time zone's offset is a multiple of 15 minutes, so the local hour can't change within a
    // slice that starts on a 15-minute UTC boundary
    private static readonly TimeSpan Slice = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan Horizon = TimeSpan.FromDays(8);

    private readonly double[] _hourShares;
    private readonly double _ordersPerDay;
    private readonly double[] _weekdayFactors;
    private readonly TimeZoneInfo _zone;

    public ArrivalSchedule(
        TimeZoneInfo zone,
        double ordersPerDay,
        IReadOnlyList<double> hourlyWeights,
        IReadOnlyList<double> weekdayWeights)
    {
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(hourlyWeights);
        ArgumentNullException.ThrowIfNull(weekdayWeights);

        if (hourlyWeights.Count != 24)
            throw new ArgumentException("There must be 24 hourly weights", nameof(hourlyWeights));

        if (weekdayWeights.Count != 7)
            throw new ArgumentException("There must be 7 weekday weights", nameof(weekdayWeights));

        _zone = zone;
        _ordersPerDay = ordersPerDay;

        // each hour's share of its day's orders
        var hourlyTotal = hourlyWeights.Sum();
        _hourShares = hourlyWeights.Select(w => hourlyTotal > 0 ? w / hourlyTotal : 0).ToArray();

        // each weekday relative to the average day, so OrdersPerDay is the average across a week
        var weekdayMean = weekdayWeights.Average();
        _weekdayFactors = weekdayWeights.Select(w => weekdayMean > 0 ? w / weekdayMean : 0).ToArray();
    }

    public static ArrivalSchedule From(CustomerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new ArrivalSchedule(
            TimeZoneInfo.FindSystemTimeZoneById(profile.TimeZone),
            profile.OrdersPerDay,
            profile.HourlyWeights,
            profile.WeekdayWeights);
    }

    /// <summary>
    ///     Expected orders per hour at <paramref name="instant" />
    /// </summary>
    public double RatePerHour(DateTimeOffset instant)
    {
        var local = TimeZoneInfo.ConvertTime(instant, _zone);

        return _ordersPerDay * _weekdayFactors[(int)local.DayOfWeek] * _hourShares[local.Hour];
    }

    /// <summary>
    ///     Time from <paramref name="now" /> until the next order arrives
    /// </summary>
    public TimeSpan NextDelay(DateTimeOffset now, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        // A unit-rate exponential draw, spent against the expected arrivals in each slice; where
        // it runs out is the next arrival. Exact for a rate that's constant within each slice.
        var remaining = -Math.Log(1.0 - random.NextDouble());
        var sliceStart = now;

        while (sliceStart - now < Horizon)
        {
            var sliceEnd = NextSliceBoundary(sliceStart);
            var rate = RatePerHour(sliceStart);
            var expectedArrivals = rate * (sliceEnd - sliceStart).TotalHours;

            if (rate > 0 && expectedArrivals >= remaining)
                return sliceStart - now + TimeSpan.FromHours(remaining / rate);

            remaining -= expectedArrivals;
            sliceStart = sliceEnd;
        }

        throw new InvalidOperationException(
            $"No order is due within {Horizon.TotalDays} days; the hourly or weekday weights may all be 0");
    }

    private static DateTimeOffset NextSliceBoundary(DateTimeOffset instant)
    {
        var sliceTicks = Slice.Ticks;
        var nextBoundary = (instant.UtcTicks / sliceTicks + 1) * sliceTicks;

        return new DateTimeOffset(nextBoundary, TimeSpan.Zero);
    }
}
