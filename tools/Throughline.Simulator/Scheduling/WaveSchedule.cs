using Throughline.Simulator.Configuration;

namespace Throughline.Simulator.Scheduling;

/// <summary>
///     When a customer's orders are released for allocation, in the customer's local time, and whether each
///     wave is before the customer's same-day cutoff
/// </summary>
internal sealed class WaveSchedule
{
    // a week covers every day a wave can fall on, plus one day for a search starting after the last wave
    private const int DaysToSearch = 8;

    private readonly TimeSpan _sameDayCutoff;
    private readonly IReadOnlyList<WaveTimesOptions> _waves;
    private readonly TimeZoneInfo _zone;

    public WaveSchedule(TimeZoneInfo zone, AllocationProfile allocation)
    {
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(allocation);

        _zone = zone;
        _waves = allocation.Waves;
        _sameDayCutoff = allocation.SameDayCutoff;
    }

    public static WaveSchedule From(CustomerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new WaveSchedule(TimeZoneInfo.FindSystemTimeZoneById(profile.TimeZone), profile.Allocation);
    }

    /// <summary>
    ///     The first wave strictly after <paramref name="after" />
    /// </summary>
    public DateTimeOffset Next(DateTimeOffset after)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(after, _zone).DateTime);

        for (var offset = 0; offset < DaysToSearch; offset++)
        {
            var day = today.AddDays(offset);

            var next = _waves
                .Where(w => w.Days.Contains(day.DayOfWeek))
                .SelectMany(w => w.Times)
                .Select(t => day.ToDateTime(TimeOnly.MinValue) + t)

                // a time skipped by the clocks going forward has no wave that day
                .Where(local => !_zone.IsInvalidTime(local))
                .Select(local => new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, _zone), TimeSpan.Zero))
                .Where(wave => wave > after)
                .Order()
                .FirstOrDefault();

            if (next != default)
                return next;
        }

        throw new InvalidOperationException($"No wave is scheduled in the {DaysToSearch} days from {today}");
    }

    /// <summary>
    ///     True when <paramref name="wave" /> is at or before the same-day cutoff, so the orders it releases can
    ///     still ship that day
    /// </summary>
    public bool IsSameDay(DateTimeOffset wave) => TimeZoneInfo.ConvertTime(wave, _zone).TimeOfDay <= _sameDayCutoff;
}
