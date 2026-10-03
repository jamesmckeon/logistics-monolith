namespace Throughline.Common.Models;

public sealed class NonFutureDateTime : AppDateTime
{
    public NonFutureDateTime(DateTimeOffset dateTime) : base(dateTime)
    {
        ThrowIfFuture(Value, nameof(dateTime));
    }

    public NonFutureDateTime(DateTime dateTime) : base(dateTime)
    {
        ThrowIfFuture(Value, nameof(dateTime));
    }

    private static void ThrowIfFuture(DateTimeOffset value, string paramName)
    {
        if (value > DateTimeOffset.UtcNow)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"{paramName} must not be in the future");
        }
    }
}
