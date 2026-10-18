using Throughline.Simulator.Configuration;
using Throughline.Simulator.Orders;

namespace Throughline.Simulator.Tests.Orders;

[Category("Unit")]
public sealed class SkuCatalogTests
{
    [Test]
    public void From_NamedAndGeneratedSkus_NamedFirstThenGeneratedInOrder()
    {
        var profile = new CatalogProfile
        {
            NamedSkus = ["HFO-JKT-ALPINE-BLK-M", "HFO-SOCK-MERINO-GRY-L"],
            GeneratedSkuPrefix = "HFO-",
            GeneratedSkuCount = 6000
        };

        var actual = SkuCatalog.From(profile);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Count, Is.EqualTo(6002));
            Assert.That(actual.Skus[0], Is.EqualTo("HFO-JKT-ALPINE-BLK-M"));
            Assert.That(actual.Skus[2], Is.EqualTo("HFO-000001"));
            Assert.That(actual.Skus[^1], Is.EqualTo("HFO-006000"));
        });
    }

    [Test]
    public void PickDistinct_SixOfTen_ReturnsDistinctCatalogSkus()
    {
        var sut = new SkuCatalog(Skus(10), 0.2, 0.8);

        var actual = sut.PickDistinct(6, new Random(3));

        Assert.Multiple(() =>
        {
            Assert.That(actual, Has.Count.EqualTo(6));
            Assert.That(actual, Is.Unique);
            Assert.That(actual, Is.SubsetOf(sut.Skus));
        });
    }

    [Test]
    public void PickDistinct_ManySingleSkuPicks_HotSkusGetConfiguredShareOfLines()
    {
        // the first 20 of 100 SKUs are "hot" and should get 80% of lines
        var skus = Skus(100);
        var hotSkus = skus.Take(20).ToHashSet();
        var sut = new SkuCatalog(skus, 0.2, 0.8);
        var random = new Random(5);

        var hotShare = Enumerable.Range(0, 10_000)
            .Count(_ => hotSkus.Contains(sut.PickDistinct(1, random)[0])) / 10_000.0;

        Assert.That(hotShare, Is.EqualTo(0.8).Within(0.02));
    }

    private static List<string> Skus(int count) =>
        Enumerable.Range(1, count).Select(n => $"SKU-{n:D3}").ToList();
}
