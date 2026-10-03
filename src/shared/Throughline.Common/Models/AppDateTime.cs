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