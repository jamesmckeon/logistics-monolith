using Throughline.Common.Models;

namespace Throughline.Common.Tests.Models;

[Category("Unit")]
public sealed class NonFutureDateTimeTests
{
    private static readonly TimeSpan Margin = TimeSpan.FromMinutes(1);

    #region Constructor

    [Test]
    public void Constructor_PastDateTimeOffset_SetsValue()
    {
        var past = DateTimeOffset.UtcNow.Subtract(Margin);

        var nonFutureDateTime = new NonFutureDateTime(past);

        Assert.That(nonFutureDateTime.Value, Is.EqualTo(past));
    }

    [Test]
    public void Constructor_CurrentDateTimeOffset_SetsValue()
    {
        var now = DateTimeOffset.UtcNow;

        var nonFutureDateTime = new NonFutureDateTime(now);

        Assert.That(nonFutureDateTime.Value, Is.EqualTo(now));
    }

    [Test]
    public void Constructor_FutureDateTimeOffset_ThrowsArgumentOutOfRangeException()
    {
        var future = DateTimeOffset.UtcNow.Add(Margin);

        Assert.That(() => new NonFutureDateTime(future), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Constructor_PastUtcDateTime_SetsValue()
    {
        var past = DateTime.UtcNow.Subtract(Margin);

        var nonFutureDateTime = new NonFutureDateTime(past);

        Assert.That(nonFutureDateTime.Value, Is.EqualTo(new DateTimeOffset(past)));
    }

    [Test]
    public void Constructor_CurrentUtcDateTime_SetsValue()
    {
        var now = DateTime.UtcNow;

        var nonFutureDateTime = new NonFutureDateTime(now);

        Assert.That(nonFutureDateTime.Value, Is.EqualTo(new DateTimeOffset(now)));
    }

    [Test]
    public void Constructor_FutureUtcDateTime_ThrowsArgumentOutOfRangeException()
    {
        var future = DateTime.UtcNow.Add(Margin);

        Assert.That(() => new NonFutureDateTime(future), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    #endregion
}
