using Throughline.Common.Models;

namespace Throughline.Common.Tests.Models;

[Category("Unit")]
public sealed class AppDateTimeTests
{
    private static readonly DateTime UtcInstant = new(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset ExpectedValue = new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);

    #region Constructor

    [Test]
    public void Constructor_UtcKind_KeepsSameInstantAtUtc()
    {
        var appDateTime = new AppDateTime(UtcInstant);

        Assert.Multiple(() =>
        {
            Assert.That(appDateTime.Value, Is.EqualTo(ExpectedValue));
            Assert.That(appDateTime.Value.Offset, Is.EqualTo(TimeSpan.Zero));
        });
    }

    [Test]
    public void Constructor_LocalKind_ConvertsSameInstantToUtc()
    {
        Assume.That(TimeZoneInfo.Local.GetUtcOffset(UtcInstant), Is.Not.EqualTo(TimeSpan.Zero));

        var localInstant = UtcInstant.ToLocalTime();

        var appDateTime = new AppDateTime(localInstant);

        Assert.Multiple(() =>
        {
            Assert.That(appDateTime.Value, Is.EqualTo(ExpectedValue));
            Assert.That(appDateTime.Value.Offset, Is.EqualTo(TimeSpan.Zero));
        });
    }

    [Test]
    public void Constructor_UnspecifiedKind_ThrowsArgumentException()
    {
        var unspecified = DateTime.SpecifyKind(UtcInstant, DateTimeKind.Unspecified);

        Assert.That(() => new AppDateTime(unspecified), Throws.ArgumentException);
    }

    #endregion
}