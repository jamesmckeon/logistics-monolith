using Throughline.Simulator.Buying;
using Throughline.Simulator.Configuration;
using Throughline.Simulator.Orders;

namespace Throughline.Simulator.Tests.Buying;

[Category("Unit")]
public sealed class PurchasePlannerTests
{
    private static readonly BuyingProfile Buying = new() { SafetyMargin = 0.2, CasePack = 12, OpeningWeeks = 3 };

    // 10 sold + 20% = 12, one case; 11 sold + 20% = 13.2, two cases
    [TestCase(10, 12)]
    [TestCase(11, 24)]
    [TestCase(1, 12)]
    public void Weekly_UnitsSold_SalesPlusMarginInWholeCases(int sold, int expected)
    {
        var unitsSold = new Dictionary<string, int> { ["HFO-000001"] = sold };

        var actual = PurchasePlanner.Weekly(unitsSold, Buying);

        Assert.That(actual, Is.EqualTo(new[] { new PurchaseLine("HFO-000001", expected) }));
    }

    [Test]
    public void Weekly_SkuWithNoSales_NotReordered()
    {
        var unitsSold = new Dictionary<string, int> { ["HFO-000001"] = 0, ["HFO-000002"] = 5 };

        var actual = PurchasePlanner.Weekly(unitsSold, Buying);

        Assert.That(actual.Select(l => l.Sku), Is.EqualTo(new[] { "HFO-000002" }));
    }

    [Test]
    public void Opening_Catalog_OrdersOnlyHotSkusInWholeCases()
    {
        // 100 SKUs, the first 20 hot
        var catalog = new SkuCatalog(Enumerable.Range(1, 100).Select(n => $"HFO-{n:D6}").ToList(), 0.2, 0.8);
        var customer = new CustomerProfile
        {
            OrdersPerDay = 600,
            LinesPerOrder = new SizeProfile { P50 = 2, P95 = 6 },
            QuantityPerLine = new SizeProfile { P50 = 1, P95 = 4 },
            Buying = Buying
        };

        var actual = PurchasePlanner.Opening(catalog, customer);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Select(l => l.Sku), Is.EqualTo(catalog.HotSkus));
            Assert.That(actual.Select(l => l.Units % 12), Is.All.EqualTo(0));
            Assert.That(actual.Select(l => l.Units), Is.All.GreaterThanOrEqualTo(12));
        });
    }
}
