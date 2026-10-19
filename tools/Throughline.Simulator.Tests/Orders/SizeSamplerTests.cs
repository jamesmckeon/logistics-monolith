using Throughline.Simulator.Configuration;
using Throughline.Simulator.Orders;

namespace Throughline.Simulator.Tests.Orders;

[Category("Unit")]
public sealed class SizeSamplerTests
{
    private static readonly SizeProfile TwoToSix = new() { P50 = 2, P95 = 6 };

    #region Sample

    [Test]
    public void Sample_P95EqualsP50_AlwaysReturnsP50()
    {
        var profile = new SizeProfile { P50 = 3, P95 = 3 };

        var actual = Draw(profile, 100, 50);

        Assert.That(actual, Is.All.EqualTo(3));
    }

    [Test]
    public void Sample_ManyDraws_MedianIsP50()
    {
        var draws = Draw(TwoToSix, 20_000, 50).Order().ToList();

        Assert.That(draws[draws.Count / 2], Is.EqualTo(2));
    }

    [Test]
    public void Sample_ManyDraws_NinetyFifthPercentileIsNearP95()
    {
        var draws = Draw(TwoToSix, 20_000, 50).Order().ToList();

        Assert.That(draws[(int)(draws.Count * 0.95)], Is.InRange(5, 7));
    }

    [Test]
    public void Sample_MaxBelowTail_StaysBetweenOneAndMax()
    {
        var actual = Draw(TwoToSix, 5_000, 4);

        Assert.That(actual, Is.All.InRange(1, 4));
    }

    #endregion

    [Test]
    public void Mean_P95EqualsP50_IsP50()
    {
        var actual = SizeSampler.Mean(new SizeProfile { P50 = 3, P95 = 3 });

        Assert.That(actual, Is.EqualTo(3));
    }

    [Test]
    public void Mean_LongTail_MatchesAverageOfManyDraws()
    {
        // the cap is set far above the tail so it doesn't pull the average down
        var draws = Draw(TwoToSix, 20_000, 1000);

        var actual = SizeSampler.Mean(TwoToSix);

        Assert.That(actual, Is.EqualTo(draws.Average()).Within(5).Percent);
    }

    private static IEnumerable<int> Draw(SizeProfile profile, int count, int max)
    {
        var random = new Random(11);
        return Enumerable.Range(0, count).Select(_ => SizeSampler.Sample(profile, random, max)).ToList();
    }
}
