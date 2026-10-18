using Throughline.Simulator.Configuration;

namespace Throughline.Simulator.Orders;

/// <summary>
///     Builds the 940s a customer's OMS sends: mostly new orders, plus resends and bad requests at
///     the profile's rates. Resends reuse orders Throughline has already accepted.
/// </summary>
internal sealed class OrderGenerator
{
    private const int RecentCapacity = 200;
    private const int MaxQuantity = 999;

    private readonly SkuCatalog _catalog;
    private readonly FaultRates _faults;

    // distinguishes this process's reference numbers from a previous run's
    private readonly string _instance;
    private readonly SizeProfile _linesPerOrder;
    private readonly string _prefix;
    private readonly SizeProfile _quantityPerLine;
    private readonly Random _random;
    private readonly List<OrderRequest> _recentlyAccepted = [];
    private readonly TimeProvider _time;
    private long _sequence;

    public OrderGenerator(CustomerProfile profile, SkuCatalog catalog, Random random, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(time);

        _prefix = profile.ReferencePrefix;
        _linesPerOrder = profile.LinesPerOrder;
        _quantityPerLine = profile.QuantityPerLine;
        _faults = profile.Faults;
        _catalog = catalog;
        _random = random;
        _time = time;
        _instance = random.Next(0x10000).ToString("X4", System.Globalization.CultureInfo.InvariantCulture);
    }

    public GeneratedOrder Next()
    {
        var kind = ChooseKind();

        return kind switch
        {
            OrderKind.IdenticalResend => new GeneratedOrder(kind, PickRecentlyAccepted()),
            OrderKind.ChangedResend => new GeneratedOrder(kind, WithChangedQuantity(PickRecentlyAccepted())),
            OrderKind.UnknownSku => new GeneratedOrder(kind, WithUnknownSku(NewOrder())),
            OrderKind.MissingRequiredField => new GeneratedOrder(kind, WithMissingField(NewOrder())),
            OrderKind.InvalidQuantity => new GeneratedOrder(kind, WithInvalidQuantity(NewOrder())),
            _ => new GeneratedOrder(OrderKind.New, NewOrder())
        };
    }

    /// <summary>
    ///     Makes an accepted order available for resending
    /// </summary>
    public void RecordAccepted(OrderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        _recentlyAccepted.Add(request);

        if (_recentlyAccepted.Count > RecentCapacity)
            _recentlyAccepted.RemoveAt(0);
    }

    private OrderKind ChooseKind()
    {
        // a resend needs an order Throughline has already accepted
        var canResend = _recentlyAccepted.Count > 0;

        (OrderKind Kind, double Rate)[] faults =
        [
            (OrderKind.IdenticalResend, canResend ? _faults.IdenticalResend : 0),
            (OrderKind.ChangedResend, canResend ? _faults.ChangedResend : 0),
            (OrderKind.UnknownSku, _faults.UnknownSku),
            (OrderKind.MissingRequiredField, _faults.MissingRequiredField),
            (OrderKind.InvalidQuantity, _faults.InvalidQuantity)
        ];

        var roll = _random.NextDouble();
        var cumulative = 0.0;

        foreach (var (kind, rate) in faults)
        {
            cumulative += rate;

            if (roll < cumulative)
                return kind;
        }

        return OrderKind.New;
    }

    private OrderRequest NewOrder()
    {
        var number = $"{_prefix}-{_time.GetUtcNow():yyyyMMdd}-{_instance}-{++_sequence:D6}";
        var address = ShipToAddresses.All[_random.Next(ShipToAddresses.All.Count)];
        var maxLines = Math.Min(SimulatorOptionsValidator.MaxLinesPerOrder, _catalog.Count);
        var lineCount = SizeSampler.Sample(_linesPerOrder, _random, maxLines);

        var items = _catalog.PickDistinct(lineCount, _random)
            .Select(sku => new OrderRequestItem(sku, SizeSampler.Sample(_quantityPerLine, _random, MaxQuantity)))
            .ToList();

        return new OrderRequest(
            $"PO-{number}", number, address.Street, address.Street2, address.City, address.State,
            address.PostalCode, items);
    }

    private OrderRequest PickRecentlyAccepted() => _recentlyAccepted[_random.Next(_recentlyAccepted.Count)];

    // same reference number, different content
    private static OrderRequest WithChangedQuantity(OrderRequest order)
    {
        var items = order.Items
            .Select((item, index) => index == 0 ? item with { Quantity = item.Quantity + 1 } : item)
            .ToList();

        return order with { Items = items };
    }

    private OrderRequest WithUnknownSku(OrderRequest order)
    {
        var unknownSku = $"{_prefix}-UNKNOWN-{_random.Next(1_000_000):D6}";

        return order with { Items = ReplaceOneLine(order.Items, item => item with { Sku = unknownSku }) };
    }

    private OrderRequest WithMissingField(OrderRequest order)
    {
        return _random.Next(5) switch
        {
            0 => order with { PurchaseOrderNumber = "" },
            1 => order with { StreetAddressOne = "" },
            2 => order with { City = "" },
            3 => order with { PostalCode = "" },
            _ => order with { Items = [] }
        };
    }

    private OrderRequest WithInvalidQuantity(OrderRequest order)
    {
        var quantity = _random.Next(2) == 0 ? 0 : -1;

        return order with { Items = ReplaceOneLine(order.Items, item => item with { Quantity = quantity }) };
    }

    private List<OrderRequestItem> ReplaceOneLine(
        IReadOnlyList<OrderRequestItem> items, Func<OrderRequestItem, OrderRequestItem> replace)
    {
        var target = _random.Next(items.Count);

        return items.Select((item, index) => index == target ? replace(item) : item).ToList();
    }
}
