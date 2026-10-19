using Throughline.Simulator.Configuration;

namespace Throughline.Simulator.Orders;

/// <summary>
///     A customer's item master, ordered most popular first. A small "hot" share of SKUs receives
///     most order lines, the long tail the rest.
/// </summary>
internal sealed class SkuCatalog
{
    private readonly int _hotCount;
    private readonly double _hotLineShare;
    private readonly IReadOnlyList<string> _skus;
    private readonly HashSet<string> _skuSet;

    public SkuCatalog(IReadOnlyList<string> skusByPopularity, double hotSkuShare, double hotLineShare)
    {
        ArgumentNullException.ThrowIfNull(skusByPopularity);

        if (skusByPopularity.Count == 0)
            throw new ArgumentException("A catalog must contain at least one SKU", nameof(skusByPopularity));

        _skus = skusByPopularity;
        _skuSet = new HashSet<string>(skusByPopularity, StringComparer.OrdinalIgnoreCase);
        _hotCount = Math.Clamp((int)Math.Round(skusByPopularity.Count * hotSkuShare), 1, skusByPopularity.Count);
        _hotLineShare = hotLineShare;
    }

    public int Count => _skus.Count;

    public IReadOnlyList<string> Skus => _skus;

    public static SkuCatalog From(IReadOnlyList<string> skusByPopularity, CatalogProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new SkuCatalog(skusByPopularity, profile.HotSkuShare, profile.HotLineShare);
    }

    public bool Contains(string sku) => _skuSet.Contains(sku);

    /// <summary>
    ///     Picks <paramref name="count" /> different SKUs; an order can't repeat a SKU
    /// </summary>
    public IReadOnlyList<string> PickDistinct(int count, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, Count);

        var picked = new List<string>(count);
        var seen = new HashSet<string>();

        while (picked.Count < count)
        {
            var sku = Pick(random);

            if (seen.Add(sku))
                picked.Add(sku);
        }

        return picked;
    }

    private string Pick(Random random)
    {
        var hot = _hotCount == Count || random.NextDouble() < _hotLineShare;

        return hot
            ? _skus[random.Next(_hotCount)]
            : _skus[_hotCount + random.Next(Count - _hotCount)];
    }
}
