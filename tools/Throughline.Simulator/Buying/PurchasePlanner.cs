using Throughline.Simulator.Configuration;
using Throughline.Simulator.Orders;

namespace Throughline.Simulator.Buying;

internal sealed record PurchaseLine(CatalogSku Sku, int Units);

/// <summary>
///     What a customer's buyer orders from its supplier, in whole cartons of each SKU
/// </summary>
internal static class PurchasePlanner
{
    /// <summary>
    ///     The weekly order: last week's sales of each SKU plus the safety margin, rounded up to the SKU's cartons.
    ///     SKUs that didn't sell, or are no longer in the catalog, aren't reordered.
    /// </summary>
    public static IReadOnlyList<PurchaseLine> Weekly(
        IReadOnlyDictionary<string, int> unitsSold, SkuCatalog catalog, BuyingProfile buying)
    {
        ArgumentNullException.ThrowIfNull(unitsSold);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(buying);

        var lines = new List<PurchaseLine>();

        foreach (var (code, sold) in unitsSold.OrderBy(s => s.Key, StringComparer.Ordinal))
        {
            if (sold <= 0 || catalog.Find(code) is not { } sku)
                continue;

            lines.Add(new PurchaseLine(sku, RoundUpToCartons(sold * (1 + buying.SafetyMargin), sku.CartonQuantity)));
        }

        return lines;
    }

    /// <summary>
    ///     The opening order, placed once before any sales: <c>OpeningWeeks</c> of each SKU's expected demand, for
    ///     the SKUs whose demand over that time fills at least one carton. The rest are first stocked by the weekly
    ///     order after they sell.
    /// </summary>
    public static IReadOnlyList<PurchaseLine> Opening(SkuCatalog catalog, CustomerProfile customer)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(customer);

        var unitsPerWeek = customer.OrdersPerDay * 7
                           * SizeSampler.Mean(customer.LinesPerOrder)
                           * SizeSampler.Mean(customer.QuantityPerLine);
        var lines = new List<PurchaseLine>();

        for (var rank = 0; rank < catalog.Count; rank++)
        {
            var sku = catalog.Skus[rank];
            var units = unitsPerWeek * catalog.Shares[rank] * customer.Buying.OpeningWeeks;

            if (units >= sku.CartonQuantity)
                lines.Add(new PurchaseLine(sku, RoundUpToCartons(units, sku.CartonQuantity)));
        }

        return lines;
    }

    private static int RoundUpToCartons(double units, int cartonQuantity) =>
        (int)Math.Ceiling(units / cartonQuantity) * cartonQuantity;
}
