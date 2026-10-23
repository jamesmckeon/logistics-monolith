using Microsoft.Extensions.Options;
using Throughline.Simulator.Configuration;
using Throughline.Simulator.Http;
using Throughline.Simulator.Scheduling;
using Throughline.Simulator.Telemetry;

namespace Throughline.Simulator.Allocation;

/// <summary>
///     The facility's outbound planners. At each of a customer's wave times, the allocator on shift reads the
///     customer's backordered and unreleased orders and allocates them in planner-sized requests
///     (<see cref="WaveRelease" />). One loop per customer, so a customer's waves never overlap.
/// </summary>
internal sealed class AllocatorWorker : BackgroundService
{
    /// <summary>
    ///     The worklists' server-set maximum, and so the most orders a wave covers
    /// </summary>
    public const int MaxWaveSize = 500;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AllocatorWorker> _logger;
    private readonly SimulatorMetrics _metrics;
    private readonly SimulatorOptions _options;
    private readonly TimeProvider _time;

    public AllocatorWorker(
        IOptions<SimulatorOptions> options,
        IHttpClientFactory httpClientFactory,
        SimulatorMetrics metrics,
        TimeProvider time,
        ILogger<AllocatorWorker> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _metrics = metrics;
        _time = time;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || _options.Facility.Allocators.Count == 0)
            return Task.CompletedTask;

        var customers = _options.Customers.Where(c => c.Value.Enabled && c.Value.Allocation.Enabled).ToList();

        return Task.WhenAll(customers.Select(c => RunCustomerAsync(c.Key, c.Value, stoppingToken)));
    }

    /// <summary>
    ///     The allocator working the facility's shift at <paramref name="instant" />, or null when no allocator
    ///     works that shift or the facility is closed
    /// </summary>
    internal static AllocatorOptions? OnShift(FacilityOptions facility, TimeZoneInfo zone, DateTimeOffset instant)
    {
        ArgumentNullException.ThrowIfNull(facility);
        ArgumentNullException.ThrowIfNull(zone);

        var local = TimeZoneInfo.ConvertTime(instant, zone);
        var shift = facility.DockShifts.FindIndex(s =>
            s.Days.Contains(local.DayOfWeek) && local.TimeOfDay >= s.Start && local.TimeOfDay < s.End);

        return shift < 0 ? null : facility.Allocators.FirstOrDefault(a => a.DockShifts.Contains(shift));
    }

    /// <summary>
    ///     The orders a wave covers, in the order they're allocated: backordered orders first, since they were
    ///     accepted earlier and may now have stock, then unreleased ones, up to <paramref name="maxOrders" />
    /// </summary>
    internal static IReadOnlyList<Guid> SelectWave(
        IEnumerable<WorklistOrder> backordered, IEnumerable<WorklistOrder> unreleased, int maxOrders)
    {
        ArgumentNullException.ThrowIfNull(backordered);
        ArgumentNullException.ThrowIfNull(unreleased);

        // an order could move between the lists between the two reads; the allocate endpoint rejects duplicates
        return backordered.Concat(unreleased).Select(o => o.OrderId).Distinct().Take(maxOrders).ToList();
    }

    private async Task RunCustomerAsync(string customer, CustomerProfile profile, CancellationToken stoppingToken)
    {
        using var _ = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Customer"] = customer,
            ["OwnerId"] = profile.OwnerId
        });

        var waves = WaveSchedule.From(profile);
        var facilityZone = TimeZoneInfo.FindSystemTimeZoneById(_options.Facility.TimeZone);

        _logger.LogInformation("Allocation for {Customer} started; first wave at {WaveTime}",
            customer, waves.Next(_time.GetUtcNow()));

        while (!stoppingToken.IsCancellationRequested)
        {
            var wave = waves.Next(_time.GetUtcNow());
            var delay = wave - _time.GetUtcNow();

            try
            {
                await Task.Delay(delay > TimeSpan.Zero ? delay : TimeSpan.Zero, _time, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var allocator = OnShift(_options.Facility, facilityZone, wave);

            if (allocator is null)
            {
                _logger.LogWarning("No allocator is on shift for {Customer}'s wave at {WaveTime}; it was skipped",
                    customer, wave);
                continue;
            }

            try
            {
                await ReleaseWaveAsync(customer, profile.OwnerId, allocator.Username, wave, waves.IsSameDay(wave),
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Allocator {Allocator} couldn't release {Customer}'s wave at {WaveTime}",
                    allocator.Username, customer, wave);
            }
        }
    }

    private async Task ReleaseWaveAsync(
        string customer, int ownerId, string allocator, DateTimeOffset wave, bool sameDay,
        CancellationToken stoppingToken)
    {
        var client = new InventoryClient(_httpClientFactory.CreateClient(ThroughlineHttpClients.ForStaff(allocator)));
        var kind = sameDay ? "same-day" : "next-day";

        WorklistResponse backordered;
        WorklistResponse unreleased;

        try
        {
            backordered = await client.GetWorklistAsync(ownerId, Worklist.Backordered, MaxWaveSize, stoppingToken);
            unreleased = await client.GetWorklistAsync(ownerId, Worklist.Unreleased, MaxWaveSize, stoppingToken);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            // timeouts and connection failures that outlasted the retries
            _logger.LogError(ex,
                "Allocator {Allocator} got no worklist for owner {OwnerId}; the {WaveKind} wave at {WaveTime} was skipped",
                allocator, ownerId, kind, wave);
            return;
        }

        if (backordered.Orders is null || unreleased.Orders is null)
        {
            var failed = backordered.Orders is null ? backordered.StatusCode : unreleased.StatusCode;
            _logger.LogError(
                "Allocator {Allocator} couldn't read the worklists for owner {OwnerId} ({StatusCode}); " +
                "the {WaveKind} wave at {WaveTime} was skipped", allocator, ownerId, (int)failed, kind, wave);
            return;
        }

        var orderIds = SelectWave(backordered.Orders, unreleased.Orders, MaxWaveSize);

        if (orderIds.Count == 0)
        {
            _logger.LogInformation(
                "Allocator {Allocator} found no orders waiting for owner {OwnerId} at the {WaveKind} wave at {WaveTime}",
                allocator, ownerId, kind, wave);
            return;
        }

        var release = await new WaveRelease(client).ReleaseAsync(ownerId, orderIds, stoppingToken);
        var context = new WaveContext(customer, ownerId, allocator, wave, sameDay, kind);

        for (var i = 0; i < release.Requests.Count; i++)
            LogRequest(context, i + 1, release.RequestsPlanned, release.Requests[i]);

        // backordered orders go first, so they're the start of the wave
        LogRelease(context, orderIds.Count, Math.Min(backordered.Orders.Count, orderIds.Count), release);
    }

    private void LogRequest(WaveContext context, int number, int planned, AllocationRequestResult request)
    {
        if (request.Response is null)
        {
            _metrics.AllocationRequestSent(context.Customer, context.SameDay, null, TimeSpan.Zero);
            _logger.LogError(request.Failure,
                "Allocator {Allocator}'s {WaveKind} wave at {WaveTime} for owner {OwnerId}: request {RequestNumber} " +
                "of {RequestCount} ({OrderCount} orders) got no response from Throughline",
                context.Allocator, context.Kind, context.Wave, context.OwnerId, number, planned,
                request.OrderIds.Count);
            return;
        }

        var response = request.Response;
        var status = (int)response.StatusCode;
        _metrics.AllocationRequestSent(context.Customer, context.SameDay, status, response.Elapsed);

        switch (WaveRelease.Classify(response.StatusCode))
        {
            case WaveResult.Allocated:
                LogAllocated(context, number, planned, request.OrderIds.Count, response);
                break;

            case WaveResult.Rejected:
                _logger.LogError(
                    "Allocator {Allocator}'s {WaveKind} wave at {WaveTime} for owner {OwnerId}: request {RequestNumber} " +
                    "of {RequestCount} ({OrderCount} orders) was rejected with {StatusCode}; the simulator built a " +
                    "bad request", context.Allocator, context.Kind, context.Wave, context.OwnerId, number, planned,
                    request.OrderIds.Count, status);
                break;

            default:
                _logger.LogError(
                    "Allocator {Allocator}'s {WaveKind} wave at {WaveTime} for owner {OwnerId}: request {RequestNumber} " +
                    "of {RequestCount} ({OrderCount} orders) got {StatusCode}",
                    context.Allocator, context.Kind, context.Wave, context.OwnerId, number, planned,
                    request.OrderIds.Count, status);
                break;
        }
    }

    private void LogAllocated(
        WaveContext context, int number, int planned, int orderCount, AllocationResponse response)
    {
        var summary = WaveSummary.From(response.Result);

        foreach (var (outcome, count) in summary.Outcomes)
            _metrics.OrdersAllocated(context.Customer, outcome, count);

        _logger.LogInformation(
            "Allocator {Allocator}'s {WaveKind} wave at {WaveTime} for owner {OwnerId}: request {RequestNumber} of " +
            "{RequestCount}, {OrderCount} orders, {FullyAllocated} fully allocated, {PartiallyAllocated} partially " +
            "allocated, {NotAllocated} not allocated, {Failed} failed, {UnitsShort} units short, in {ElapsedMs:F0} ms",
            context.Allocator, context.Kind, context.Wave, context.OwnerId, number, planned, orderCount,
            summary.Count(WaveSummary.FullyAllocated),
            summary.Count(WaveSummary.PartiallyAllocated),
            summary.Count(WaveSummary.NotAllocated),
            summary.Count(WaveSummary.Failed),
            summary.UnitsShort,
            response.Elapsed.TotalMilliseconds);

        if (summary.ErrorCodes.Count > 0)
            _logger.LogWarning(
                "Allocator {Allocator}'s {WaveKind} wave at {WaveTime} for owner {OwnerId}: request {RequestNumber} " +
                "had failed orders: {ErrorCodes}",
                context.Allocator, context.Kind, context.Wave, context.OwnerId, number,
                string.Join(", ", summary.ErrorCodes.Select(e => $"{e.Key} x{e.Value}")));
    }

    private void LogRelease(WaveContext context, int orderCount, int backorderedCount, WaveReleaseResult release)
    {
        if (release.Finished)
        {
            _logger.LogInformation(
                "Allocator {Allocator} released the {WaveKind} wave at {WaveTime} for owner {OwnerId}: " +
                "{OrdersReleased} of {OrderCount} orders ({BackorderedCount} backordered) in {RequestsSent} of " +
                "{RequestCount} requests, finished",
                context.Allocator, context.Kind, context.Wave, context.OwnerId, release.OrdersReleased, orderCount,
                backorderedCount, release.Requests.Count, release.RequestsPlanned);
            return;
        }

        _logger.LogWarning(
            "Allocator {Allocator} stopped the {WaveKind} wave at {WaveTime} for owner {OwnerId} early: " +
            "{OrdersReleased} of {OrderCount} orders ({BackorderedCount} backordered) in {RequestsSent} of " +
            "{RequestCount} requests; the rest wait for the next wave",
            context.Allocator, context.Kind, context.Wave, context.OwnerId, release.OrdersReleased, orderCount,
            backorderedCount, release.Requests.Count, release.RequestsPlanned);
    }

    private sealed record WaveContext(
        string Customer, int OwnerId, string Allocator, DateTimeOffset Wave, bool SameDay, string Kind);
}

/// <summary>
///     What an allocation request came to: orders per allocation outcome, units still short, and why orders failed
/// </summary>
internal sealed class WaveSummary
{
    public const string FullyAllocated = "fullyAllocated";
    public const string PartiallyAllocated = "partiallyAllocated";
    public const string NotAllocated = "notAllocated";
    public const string Failed = "failed";

    private WaveSummary(
        IReadOnlyDictionary<string, int> outcomes, int unitsShort, IReadOnlyDictionary<string, int> errorCodes)
    {
        Outcomes = outcomes;
        UnitsShort = unitsShort;
        ErrorCodes = errorCodes;
    }

    /// <summary>
    ///     Orders per allocation outcome, keyed by the endpoint's status
    /// </summary>
    public IReadOnlyDictionary<string, int> Outcomes { get; }

    public int UnitsShort { get; }

    /// <summary>
    ///     Failed orders per error code
    /// </summary>
    public IReadOnlyDictionary<string, int> ErrorCodes { get; }

    public int Count(string outcome) => Outcomes.GetValueOrDefault(outcome);

    public static WaveSummary From(AllocationResult? result)
    {
        var orders = result?.Orders ?? [];

        return new WaveSummary(
            orders.GroupBy(o => o.Status).ToDictionary(g => g.Key, g => g.Count()),
            orders.SelectMany(o => o.Shortages).Sum(s => s.QuantityOutstanding),
            orders.SelectMany(o => o.Errors).GroupBy(e => e.Code).ToDictionary(g => g.Key, g => g.Count()));
    }
}
