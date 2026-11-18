using Throughline.Simulator.Configuration;
using Throughline.Simulator.Orders;

namespace Throughline.Simulator.Tests.Orders;

[Category("Unit")]
public sealed class SkuCatalogTests
{
    [Test]
    public void From_CompleteItemMaster_KeepsSkusInPopularityOrder()
    {
        var itemMaster = Enumerable.Range(1, 50).Select(n => new ItemMasterSku($"SKU-{n:D3}", 12, 360)).ToList();

        var actual = SkuCatalog.From("HarrowFinch", itemMaster, new CatalogProfile { ZipfExponent = 1 });

        Assert.That(actual.Skus.Select(s => s.Code), Is.EqualTo(itemMaster.Select(s => s.Code)));
    }

    [Test]
    public void From_SkusMissingQuantities_ThrowsNamingCustomerAndSkus()
    {
        ItemMasterSku[] itemMaster =
        [
            new("SKU-001", 12, 360),
            new("SKU-002", 12, null),
            new("SKU-003", null, null)
        ];

        var actual = Assert.Throws<InvalidOperationException>(() =>
            SkuCatalog.From("HarrowFinch", itemMaster, new CatalogProfile { ZipfExponent = 1 }));

        Assert.That(actual.Message, Does.Contain("HarrowFinch").And.Contain("SKU-002, SKU-003"));
    }

    [Test]
    public void Constructor_NoSkus_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _ = new SkuCatalog([], 1));
    }

    [Test]
    public void Shares_ExponentOne_ProportionalToOneOverRank()
    {
        // weights 1, 1/2 and 1/3 add up to 11/6
        var sut = new SkuCatalog(Skus(3), 1);

        var actual = sut.Shares;

        Assert.That(actual, Is.EqualTo(new[] { 6 / 11.0, 3 / 11.0, 2 / 11.0 }).Within(1e-9));
    }

    #region PickDistinct

    [Test]
    public void PickDistinct_SixOfTen_ReturnsDistinctCatalogSkus()
    {
        var sut = new SkuCatalog(Skus(10), 1);

        var actual = sut.PickDistinct(6, new Random(3));

        Assert.Multiple(() =>
        {
            Assert.That(actual, Has.Count.EqualTo(6));
            Assert.That(actual, Is.Unique);
            Assert.That(actual, Is.SubsetOf(sut.Skus));
        });
    }

    [Test]
    public void PickDistinct_AllOfThem_ReturnsEveryCatalogSku()
    {
        var sut = new SkuCatalog(Skus(5), 1);

        var actual = sut.PickDistinct(5, new Random(3));

        Assert.That(actual, Is.EquivalentTo(sut.Skus));
    }

    [Test]
    public void PickDistinct_ManySingleSkuPicks_TopSkuGetsItsShareOfLines()
    {
        // with exponent 1, the top of 100 SKUs gets 1 / (1 + 1/2 + ... + 1/100), about 19.3% of lines
        var sut = new SkuCatalog(Skus(100), 1);
        var top = sut.Skus[0];
        var random = new Random(5);

        var topShare = Enumerable.Range(0, 20_000).Count(_ => sut.PickDistinct(1, random)[0] == top) / 20_000.0;

        Assert.That(topShare, Is.EqualTo(sut.Shares[0]).Within(0.01));
    }

    #endregion

    [Test]
    public void Find_KnownCodeInOtherCase_ReturnsSku()
    {
        var sut = new SkuCatalog(Skus(3), 1);

        var actual = sut.Find("sku-002");

        Assert.That(actual, Is.EqualTo(new CatalogSku("SKU-002", 12, 360)));
    }

    [Test]
    public void Find_UnknownCode_ReturnsNull()
    {
        var sut = new SkuCatalog(Skus(3), 1);

        var actual = sut.Find("SKU-999");

        Assert.That(actual, Is.Null);
    }

    private static List<CatalogSku> Skus(int count) =>
        Enumerable.Range(1, count).Select(n => new CatalogSku($"SKU-{n:D3}", 12, 360)).ToList();
}
