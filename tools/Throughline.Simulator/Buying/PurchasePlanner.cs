using Throughline.Simulator.Configuration;
using Throughline.Simulator.Orders;

namespace Throughline.Simulator.Buying;

internal sealed record PurchaseLine(string Sku, int Units);

/// <summary>
///     What a customer's buyer orders from its supplier
/// </summary>
internal static class PurchasePlanner
{
    /// <summary>
    ///     The weekly order: last week's sales of each SKU plus the safety margin, in whole cases.
    ///     SKUs that didn't sell aren't reordered.
    /// </summary>
    public static IReadOnlyList<PurchaseLine> Weekly(IReadOnlyDictionary<string, int> unitsSold, BuyingProfile buying)
    {
        ArgumentNullException.ThrowIfNull(unitsSold);
        ArgumentNullException.ThrowIfNull(buying);

        return unitsSold
            .Where(sold => sold.Value > 0)
            .OrderBy(sold => sold.Key, StringComparer.Ordinal)
            .Select(sold => new PurchaseLine(
                sold.Key, RoundUpToCases(sold.Value * (1 + buying.SafetyMargin), buying.CasePack)))
            .ToList();
    }

    /// <summary>
    ///     The opening order, placed once before any sales: <c>OpeningWeeks</c> of each popular SKU's
    ///     expected demand. Slow movers are first stocked by the weekly order after they sell.
    /// </summary>
    public static IReadOnlyList<PurchaseLine> Opening(SkuCatalog catalog, CustomerProfile customer)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(customer);

        var hotSkus = catalog.HotSkus;
        var unitsPerWeek = customer.OrdersPerDay * 7
                           * SizeSampler.Mean(customer.LinesPerOrder)
                           * SizeSampler.Mean(customer.QuantityPerLine);
        var unitsPerHotSku = unitsPerWeek * catalog.HotLineShare / hotSkus.Count * customer.Buying.OpeningWeeks;
        var units = Math.Max(customer.Buying.CasePack, RoundUpToCases(unitsPerHotSku, customer.Buying.CasePack));

        return hotSkus.Select(sku => new PurchaseLine(sku, units)).ToList();
    }

    private static int RoundUpToCases(double units, int casePack) =>
        (int)Math.Ceiling(units / casePack) * casePack;
}
