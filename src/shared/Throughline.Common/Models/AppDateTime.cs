namespace Throughline.Common.Models;

public class AppDateTime : ValueObject, IComparable<AppDateTime>
{
    public AppDateTime(DateTimeOffset dateTime)
    {
        if (dateTime.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("dateTime must be at UTC +0");
        }

        Value = dateTime;
    }

    public AppDateTime(DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Unspecified)
        {
            throw new ArgumentException("dateTime must have a Kind of Utc or Local", nameof(dateTime));
        }

        Value = new DateTimeOffset(dateTime.ToUniversalTime());
    }

    public DateTimeOffset Value { get; }

    public static NonFutureDateTime Now => new(DateTimeOffset.UtcNow);

    public int CompareTo(AppDateTime? other) => other is null ? 1 : Value.CompareTo(other.Value);

    /// <summary>
    ///     Creates an AppDateTime from a <see cref="DateTime" /> whose clock value the caller knows to be UTC, but
    ///     which may not be marked as such; e.g. a date deserialized from JSON without a time zone
    /// </summary>
    /// <exception cref="ArgumentException">
    ///     <paramref name="dateTime" /> has a Kind of Local;
    /// </exception>
    public static AppDateTime AssumeUtc(DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Local)
        {
            throw new ArgumentException("dateTime must have a Kind of Unspecified or Utc", nameof(dateTime));
        }

        return new AppDateTime(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
    }


    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public static bool operator >(AppDateTime left, AppDateTime right) => left.Value > right.Value;

    public static bool operator <(AppDateTime left, AppDateTime right) => left.Value < right.Value;

    public AppDateTime Add(TimeSpan timeSpan) => new(Value.Add(timeSpan));

    public AppDateTime Subtract(TimeSpan timeSpan) => new(Value.Subtract(timeSpan));

    public static bool TryParse(DateTime value, out AppDateTime? appDateTime) => throw new NotImplementedException();
}