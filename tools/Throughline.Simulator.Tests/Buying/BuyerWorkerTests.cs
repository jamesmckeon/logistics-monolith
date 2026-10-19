using System.Globalization;
using Throughline.Simulator.Buying;
using Throughline.Simulator.Configuration;

namespace Throughline.Simulator.Tests.Buying;

[Category("Unit")]
public sealed class BuyerWorkerTests
{
    private static readonly TimeZoneInfo Chicago = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    private static readonly BuyingProfile MondayNineAm =
        new() { ReviewDay = DayOfWeek.Monday, ReviewTime = TimeSpan.FromHours(9) };

    // Chicago is UTC-5 until 2026-11-01: Monday 2026-10-19 09:00 local is 14:00Z
    [TestCase("2026-10-19T13:00:00Z", "2026-10-19T14:00:00Z", TestName = "NextReview_MondayBeforeNine_SameMorning")]
    [TestCase("2026-10-19T14:00:00Z", "2026-10-26T14:00:00Z", TestName = "NextReview_MondayAtNine_FollowingMonday")]
    [TestCase("2026-10-21T18:00:00Z", "2026-10-26T14:00:00Z", TestName = "NextReview_Wednesday_FollowingMonday")]
    public void NextReview_After_FirstMondayNineAmStrictlyAfter(string after, string expected)
    {
        var actual = BuyerWorker.NextReview(Parse(after), MondayNineAm, Chicago);

        Assert.That(actual, Is.EqualTo(Parse(expected)));
    }

    [Test]
    public void NextReview_AcrossDaylightSavingEnd_StillNineAmLocal()
    {
        // Wednesday 2026-10-28; the next Monday, 2026-11-02, is on standard time (UTC-6)
        var actual = BuyerWorker.NextReview(Parse("2026-10-28T18:00:00Z"), MondayNineAm, Chicago);

        Assert.That(actual, Is.EqualTo(Parse("2026-11-02T15:00:00Z")));
    }

    private static DateTimeOffset Parse(string value) => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
}
