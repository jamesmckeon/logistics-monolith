using Microsoft.Extensions.Options;
using Throughline.Simulator.Configuration;
using Throughline.Simulator.Ledger;
using Throughline.Simulator.Orders;
using Throughline.Simulator.Scheduling;

namespace Throughline.Simulator.Buying;

/// <summary>
///     Each buying customer's buyer: an opening order the first time, then a weekly order replacing
///     what sold. Orders become trailers in the ledger, which the receivers pick up when they arrive.
/// </summary>
internal sealed class BuyerWorker : BackgroundService
{
    private static readonly TimeSpan RetryAfterFailure = TimeSpan.FromMinutes(15);

    private readonly ISkuCatalogSource _catalogSource;
    private readonly ILedger _ledger;
    private readonly ILogger<BuyerWorker> _logger;
    private readonly SimulatorOptions _options;
    private readonly TimeProvider _time;

    public BuyerWorker(
        IOptions<SimulatorOptions> options,
        ISkuCatalogSource catalogSource,
        ILedger ledger,
        TimeProvider time,
        ILogger<BuyerWorker> logger)
    {
        _options = options.Value;
        _catalogSource = catalogSource;
        _ledger = ledger;
        _time = time;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return Task.CompletedTask;

        var buyers = _options.Customers.Where(c => c.Value is { Enabled: true, Buying.Enabled: true });

        return Task.WhenAll(buyers.Select(c => RunBuyerAsync(c.Key, c.Value, stoppingToken)));
    }

    private async Task RunBuyerAsync(string customer, CustomerProfile profile, CancellationToken stoppingToken)
    {
        using var _ = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Customer"] = customer,
            ["OwnerId"] = profile.OwnerId
        });

        var planner = new ShipmentPlanner(
            profile.Buying, _options.Facility, DockCalendar.From(_options.Facility), new Random());
        var customerZone = TimeZoneInfo.FindSystemTimeZoneById(profile.TimeZone);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var lastOrderedAt = await _ledger.GetLastPurchaseOrderAtAsync(profile.OwnerId, stoppingToken);

                if (lastOrderedAt is null)
                {
                    await PlaceOpeningOrderAsync(customer, profile, planner, stoppingToken);
                    continue;
                }

                var reviewAt = NextReview(lastOrderedAt.Value, profile.Buying, customerZone);
                var wait = reviewAt - _time.GetUtcNow();

                if (wait > TimeSpan.Zero)
                {
                    _logger.LogInformation("Next purchase order review for {Customer} at {ReviewAt:O}", customer, reviewAt);
                    await Task.Delay(wait, _time, stoppingToken);
                }

                await PlaceWeeklyOrderAsync(customer, profile, planner, lastOrderedAt.Value, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Purchase order review for {Customer} failed; retrying in {Delay}",
                    customer, RetryAfterFailure);
                await Task.Delay(RetryAfterFailure, _time, stoppingToken);
            }
        }
    }

    private async Task PlaceOpeningOrderAsync(
        string customer, CustomerProfile profile, ShipmentPlanner planner, CancellationToken stoppingToken)
    {
        var skus = await _catalogSource.GetSkusAsync(profile.OwnerId, stoppingToken);
        var lines = PurchasePlanner.Opening(SkuCatalog.From(skus, profile.Catalog), profile);

        await SaveAsync(customer, profile, planner, PurchaseOrderKind.Opening, lines, stoppingToken);
    }

    private async Task PlaceWeeklyOrderAsync(
        string customer, CustomerProfile profile, ShipmentPlanner planner, DateTimeOffset since,
        CancellationToken stoppingToken)
    {
        var unitsSold = await _ledger.GetUnitsSoldAsync(profile.OwnerId, since, _time.GetUtcNow(), stoppingToken);
        var lines = PurchasePlanner.Weekly(unitsSold, profile.Buying);

        // nothing sold: an order with no lines still records the review, so the next one is a week away
        await SaveAsync(customer, profile, planner, PurchaseOrderKind.Weekly, lines, stoppingToken);
    }

    private async Task SaveAsync(
        string customer, CustomerProfile profile, ShipmentPlanner planner, PurchaseOrderKind kind,
        IReadOnlyList<PurchaseLine> lines, CancellationToken stoppingToken)
    {
        var order = planner.Plan(profile.OwnerId, profile.ReferencePrefix, kind, lines, _time.GetUtcNow());
        await _ledger.SavePurchaseOrderAsync(order, stoppingToken);

        _logger.LogInformation(
            "{Kind} purchase order {PurchaseOrderNumber} for {Customer}: {SkuCount} SKUs, {Units} units, " +
            "{Pallets} pallets on {Trailers} trailers, first due {FirstDueAt:O}",
            kind, order.PurchaseOrderNumber, customer, lines.Count, lines.Sum(l => l.Units),
            order.Shipments.Sum(s => s.Pallets.Count), order.Shipments.Count,
            order.Shipments.Count == 0 ? (DateTimeOffset?)null : order.Shipments.Min(s => s.DueAt));
    }

    /// <summary>
    ///     The first weekly review time strictly after <paramref name="after" />, in the customer's time zone
    /// </summary>
    internal static DateTimeOffset NextReview(DateTimeOffset after, BuyingProfile buying, TimeZoneInfo zone)
    {
        var local = TimeZoneInfo.ConvertTime(after, zone);
        var daysAhead = ((int)buying.ReviewDay - (int)local.DayOfWeek + 7) % 7;
        var candidate = local.Date.AddDays(daysAhead) + buying.ReviewTime;

        if (candidate <= local.DateTime)
            candidate = candidate.AddDays(7);

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(candidate, zone), TimeSpan.Zero);
    }
}
