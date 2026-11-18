using Throughline.Simulator.Buying;
using Throughline.Simulator.Configuration;
using Throughline.Simulator.Orders;

namespace Throughline.Simulator.Tests.Buying;

[Category("Unit")]
public sealed class PurchasePlannerTests
{
    private static readonly CatalogSku Jacket = new("HFO-000001", 24, 720);
    private static readonly CatalogSku Tee = new("HFO-000002", 12, 360);
    private static readonly CatalogSku Sock = new("HFO-000003", 36, 2160);

    // with exponent 1, the three SKUs get 6/11, 3/11 and 2/11 of lines
    private static readonly SkuCatalog Catalog = new([Jacket, Tee, Sock], 1);

    #region Weekly

    // 10 sold + 20% = 12, one carton of 12; 11 sold + 20% = 13.2, two cartons; 10 sold + 20% in cartons of 24, one
    [TestCase(10, 12, 12)]
    [TestCase(11, 12, 24)]
    [TestCase(1, 12, 12)]
    [TestCase(10, 24, 24)]
    public void Weekly_UnitsSold_SalesPlusMarginInWholeCartons(int sold, int cartonQuantity, int expected)
    {
        var sku = new CatalogSku("HFO-000001", cartonQuantity, cartonQuantity * 30);
        var unitsSold = new Dictionary<string, int> { [sku.Code] = sold };

        var actual = PurchasePlanner.Weekly(unitsSold, new SkuCatalog([sku], 1), Buying(openingWeeks: 1));

        Assert.That(actual, Is.EqualTo(new[] { new PurchaseLine(sku, expected) }));
    }

    [Test]
    public void Weekly_SkuWithNoSales_NotReordered()
    {
        var unitsSold = new Dictionary<string, int> { [Jacket.Code] = 0, [Tee.Code] = 5 };

        var actual = PurchasePlanner.Weekly(unitsSold, Catalog, Buying(openingWeeks: 1));

        Assert.That(actual.Select(l => l.Sku), Is.EqualTo(new[] { Tee }));
    }

    [Test]
    public void Weekly_SkuNoLongerInCatalog_NotReordered()
    {
        var unitsSold = new Dictionary<string, int> { ["HFO-RETIRED"] = 5, [Tee.Code] = 5 };

        var actual = PurchasePlanner.Weekly(unitsSold, Catalog, Buying(openingWeeks: 1));

        Assert.That(actual.Select(l => l.Sku), Is.EqualTo(new[] { Tee }));
    }

    #endregion

    [Test]
    public void Opening_OneWeek_StocksSkusWithACartonOfDemandInWholeCartons()
    {
        // 154 units a week: 84 jackets (4 cartons of 24 = 96), 42 tees (4 of 12 = 48), 28 socks (under a carton of 36)
        var actual = PurchasePlanner.Opening(Catalog, Customer(ordersPerDay: 22, openingWeeks: 1));

        Assert.That(actual, Is.EqualTo(new[] { new PurchaseLine(Jacket, 96), new PurchaseLine(Tee, 48) }));
    }

    [Test]
    public void Opening_ThreeWeeks_StocksThreeWeeksOfDemand()
    {
        // 462 units: 252 jackets (11 cartons = 264), 126 tees (11 cartons = 132), 84 socks (3 cartons = 108)
        var actual = PurchasePlanner.Opening(Catalog, Customer(ordersPerDay: 22, openingWeeks: 3));

        Assert.That(actual, Is.EqualTo(new[]
        {
            new PurchaseLine(Jacket, 264), new PurchaseLine(Tee, 132), new PurchaseLine(Sock, 108)
        }));
    }

    private static BuyingProfile Buying(int openingWeeks) => new() { SafetyMargin = 0.2, OpeningWeeks = openingWeeks };

    // one line of one unit per order, so units a week = orders a day x 7
    private static CustomerProfile Customer(double ordersPerDay, int openingWeeks) => new()
    {
        OrdersPerDay = ordersPerDay,
        LinesPerOrder = new SizeProfile { P50 = 1, P95 = 1 },
        QuantityPerLine = new SizeProfile { P50 = 1, P95 = 1 },
        Buying = Buying(openingWeeks)
    };
}
