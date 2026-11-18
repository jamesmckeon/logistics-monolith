using Throughline.Simulator.Configuration;

namespace Throughline.Simulator.Orders;

/// <summary>
///     A SKU the simulator can order, buy and ship
/// </summary>
/// <param name="CartonQuantity">Units (eaches) in one carton; purchases are rounded up to whole cartons</param>
/// <param name="PalletQuantity">Units (eaches) on one full single-SKU pallet; a whole number of cartons</param>
internal sealed record CatalogSku(string Code, int CartonQuantity, int PalletQuantity);

/// <summary>
///     A customer's item master, ordered most popular first. Demand tapers smoothly with popularity: the SKU at rank
///     <c>r</c> gets order lines in proportion to <c>1 / r^s</c>, where <c>s</c> is the Zipf exponent.
/// </summary>
internal sealed class SkuCatalog
{
    private readonly Dictionary<string, CatalogSku> _byCode;

    // _cumulative[i] is the share of lines going to the SKUs ranked 1 to i + 1; the last is 1
    private readonly double[] _cumulative;
    private readonly double[] _shares;

    public SkuCatalog(IReadOnlyList<CatalogSku> skusByPopularity, double zipfExponent)
    {
        ArgumentNullException.ThrowIfNull(skusByPopularity);

        if (skusByPopularity.Count == 0)
            throw new ArgumentException("A catalog must contain at least one SKU", nameof(skusByPopularity));

        Skus = skusByPopularity;
        _byCode = skusByPopularity.ToDictionary(s => s.Code, StringComparer.OrdinalIgnoreCase);

        var weights = Enumerable.Range(1, skusByPopularity.Count).Select(rank => 1 / Math.Pow(rank, zipfExponent)).ToArray();
        var total = weights.Sum();
        _shares = weights.Select(w => w / total).ToArray();

        _cumulative = new double[_shares.Length];
        var running = 0.0;

        for (var i = 0; i < _shares.Length; i++)
        {
            running += _shares[i];
            _cumulative[i] = running;
        }

        // rounding can leave the sum a hair under 1, which a roll of 0.9999... would fall past
        _cumulative[^1] = 1;
    }

    public int Count => Skus.Count;

    public IReadOnlyList<CatalogSku> Skus { get; }

    /// <summary>
    ///     Each SKU's expected share of order lines, in the same order as <see cref="Skus" />; they add up to 1
    /// </summary>
    public IReadOnlyList<double> Shares => _shares;

    /// <summary>
    ///     The customer's catalog from its item master. Every SKU needs both a carton and a pallet quantity, since the
    ///     simulator buys in cartons and ships on pallets.
    /// </summary>
    /// <exception cref="InvalidOperationException">A SKU is missing its carton or pallet quantity</exception>
    public static SkuCatalog From(string customer, IReadOnlyList<ItemMasterSku> itemMaster, CatalogProfile profile)
    {
        ArgumentNullException.ThrowIfNull(itemMaster);
        ArgumentNullException.ThrowIfNull(profile);

        var skus = new List<CatalogSku>(itemMaster.Count);
        var incomplete = new List<string>();

        foreach (var sku in itemMaster)
        {
            if (sku is { CartonQuantity: { } carton, PalletQuantity: { } pallet })
                skus.Add(new CatalogSku(sku.Code, carton, pallet));
            else
                incomplete.Add(sku.Code);
        }

        if (incomplete.Count > 0)
            throw new InvalidOperationException(
                $"{incomplete.Count} of {customer}'s SKUs have no carton quantity or no pallet quantity in the item " +
                $"master, and the simulator handles only SKUs with both: {string.Join(", ", incomplete)}");

        return new SkuCatalog(skus, profile.ZipfExponent);
    }

    public bool Contains(string code) => _byCode.ContainsKey(code);

    /// <summary>
    ///     The SKU with this code, or null when the catalog doesn't have it
    /// </summary>
    public CatalogSku? Find(string code) => _byCode.GetValueOrDefault(code);

    /// <summary>
    ///     Picks <paramref name="count" /> different SKUs, each by its share of lines; an order can't repeat a SKU
    /// </summary>
    public IReadOnlyList<CatalogSku> PickDistinct(int count, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, Count);

        var picked = new List<CatalogSku>(count);
        var seen = new HashSet<int>();

        while (picked.Count < count)
        {
            var rank = Pick(random);

            if (seen.Add(rank))
                picked.Add(Skus[rank]);
        }

        return picked;
    }

    // the first SKU whose cumulative share is above the roll
    private int Pick(Random random)
    {
        var roll = random.NextDouble();
        var index = Array.BinarySearch(_cumulative, roll);

        // an exact match is the end of that SKU's range, so the roll belongs to the next one
        index = index >= 0 ? index + 1 : ~index;

        return Math.Min(index, Count - 1);
    }
}
