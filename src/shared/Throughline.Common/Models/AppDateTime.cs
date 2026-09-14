namespace Throughline.Common.Models;

public sealed class AppDateTime : ValueObject, IComparable<AppDateTime>
{
    public AppDateTime(DateTimeOffset dateTime)
    {
        if (dateTime.Offset != TimeSpan.Zero)
            throw new ArgumentException("dateTime must be at UTC +0");

        Value = dateTime;
    }

    public DateTimeOffset Value { get; }

    public static AppDateTime Now => new(DateTimeOffset.UtcNow);

    public int CompareTo(AppDateTime? other)
    {
        return other is null ? 1 : Value.CompareTo(other.Value);
    }


    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }
}