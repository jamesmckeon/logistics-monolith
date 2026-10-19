using Throughline.Simulator.Configuration;

namespace Throughline.Simulator.Scheduling;

/// <summary>
///     When the facility's receiving dock is staffed, in the facility's local time. A shift starts and
///     ends on the same day.
/// </summary>
internal sealed class DockCalendar
{
    private const int DaysToSearch = 14;

    private readonly IReadOnlyList<ShiftOptions> _shifts;
    private readonly TimeZoneInfo _zone;

    public DockCalendar(TimeZoneInfo zone, IReadOnlyList<ShiftOptions> shifts)
    {
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(shifts);

        _zone = zone;
        _shifts = shifts;
    }

    public TimeZoneInfo Zone => _zone;

    public static DockCalendar From(FacilityOptions facility)
    {
        ArgumentNullException.ThrowIfNull(facility);

        return new DockCalendar(TimeZoneInfo.FindSystemTimeZoneById(facility.TimeZone), facility.DockShifts);
    }

    public bool IsOpen(DateTimeOffset instant)
    {
        var local = TimeZoneInfo.ConvertTime(instant, _zone);

        return _shifts.Any(s =>
            s.Days.Contains(local.DayOfWeek) && local.TimeOfDay >= s.Start && local.TimeOfDay < s.End);
    }

    /// <summary>
    ///     A random moment while the dock is staffed on <paramref name="date" />, or on the first later
    ///     date it's staffed: when a trailer due that day pulls in
    /// </summary>
    public DateTimeOffset ArrivalOn(DateOnly date, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        for (var offset = 0; offset < DaysToSearch; offset++)
        {
            var day = date.AddDays(offset);
            var shifts = _shifts.Where(s => s.Days.Contains(day.DayOfWeek)).ToList();
            var openMinutes = shifts.Sum(s => (s.End - s.Start).TotalMinutes);

            if (openMinutes <= 0)
                continue;

            // spread arrivals evenly over all of the day's open time
            var minute = random.NextDouble() * openMinutes;

            foreach (var shift in shifts)
            {
                var length = (shift.End - shift.Start).TotalMinutes;

                if (minute < length)
                {
                    var local = day.ToDateTime(TimeOnly.MinValue) + shift.Start + TimeSpan.FromMinutes(minute);
                    return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, _zone), TimeSpan.Zero);
                }

                minute -= length;
            }
        }

        throw new InvalidOperationException($"The dock isn't staffed on any day in the {DaysToSearch} days from {date}");
    }
}
