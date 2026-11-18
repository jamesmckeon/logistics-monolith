using Microsoft.Extensions.Options;
using Throughline.Simulator.Configuration;
using Throughline.Simulator.Http;
using Throughline.Simulator.Scheduling;
using Throughline.Simulator.Telemetry;

namespace Throughline.Simulator.Allocation;

/// <summary>
///     The facility's outbound planners. At each of a customer's wave times, the allocator on shift reads the
///     customer's backordered and unreleased orders and allocates them in planner-sized requests
///     (<see cref="WaveRelease" />), then keeps reading the unreleased list until everything waiting is covered.
///     One loop per customer, so a customer's waves never overlap.
/// </summary>
internal sealed class AllocatorWorker : BackgroundService
{
    /// <summary>
    ///     The worklists' server-set maximum: the most orders one read of a worklist returns
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
        var context = new WaveContext(customer, ownerId, allocator, wave, sameDay, sameDay ? "same-day" : "next-day");

        var backordered = await ReadWorklistAsync(context, client, Worklist.Backordered, stoppingToken);
        var unreleased = backordered is null
            ? null
            : await ReadWorklistAsync(context, client, Worklist.Unreleased, stoppingToken);

        if (backordered is null || unreleased is null)
        {
            _logger.LogError("Allocator {Allocator}'s {WaveKind} wave at {WaveTime} for owner {OwnerId} was skipped",
                context.Allocator, context.Kind, context.Wave, context.OwnerId);
            return;
        }

        var batch = SelectWave(backordered, unreleased, MaxWaveSize);

        if (batch.Count == 0)
        {
            _logger.LogInformation(
                "Allocator {Allocator} found no orders waiting for owner {OwnerId} at the {WaveKind} wave at {WaveTime}",
                allocator, ownerId, context.Kind, wave);
            return;
        }

        // backordered orders go first, so they're the start of the first batch
        var progress = new WaveProgress(Math.Min(backordered.Count, batch.Count));
        var release = new WaveRelease(client);

        // The backordered list is read once: orders still short stay on it, so reading it again would return them
        // forever. The unreleased list is read again after each batch until it has nothing new.
        while (batch.Count > 0)
        {
            var result = await release.ReleaseAsync(ownerId, batch, stoppingToken);

            foreach (var request in result.Requests)
                LogRequest(context, ++progress.RequestsSent, request);

            progress.Add(batch, result.OrdersReleased);

            if (!result.Finished)
                break;

            var more = await ReadWorklistAsync(context, client, Worklist.Unreleased, stoppingToken);

            if (more is null)
                break;

            batch = NextBatch(more, progress.Covered, MaxWaveSize);

            if (batch.Count == 0)
                progress.Finished = true;
        }

        LogRelease(context, progress);
    }

    /// <summary>
    ///     The orders on a later read of the unreleased list that this wave hasn't already sent, up to
    ///     <paramref name="maxOrders" />. An order the endpoint couldn't allocate can stay on the list, and sending it
    ///     again in the same wave would loop forever.
    /// </summary>
    internal static IReadOnlyList<Guid> NextBatch(
        IEnumerable<WorklistOrder> unreleased, IReadOnlySet<Guid> alreadySent, int maxOrders)
    {
        ArgumentNullException.ThrowIfNull(unreleased);
        ArgumentNullException.ThrowIfNull(alreadySent);

        return unreleased.Select(o => o.OrderId).Distinct().Where(id => !alreadySent.Contains(id)).Take(maxOrders)
            .ToList();
    }

    // one worklist, or null when it couldn't be read
    private async Task<IReadOnlyList<WorklistOrder>?> ReadWorklistAsync(
        WaveContext context, IInventoryClient client, Worklist worklist, CancellationToken stoppingToken)
    {
        WorklistResponse response;

        try
        {
            response = await client.GetWorklistAsync(context.OwnerId, worklist, MaxWaveSize, stoppingToken);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            // timeouts and connection failures that outlasted the retries
            _logger.LogError(ex,
                "Allocator {Allocator} got no {Worklist} worklist for owner {OwnerId} at the {WaveKind} wave at {WaveTime}",
                context.Allocator, worklist, context.OwnerId, context.Kind, context.Wave);
            return null;
        }

        if (response.Orders is null)
            _logger.LogError(
                "Allocator {Allocator} couldn't read the {Worklist} worklist for owner {OwnerId} ({StatusCode}) at the " +
                "{WaveKind} wave at {WaveTime}",
                context.Allocator, worklist, context.OwnerId, (int)response.StatusCode, context.Kind, context.Wave);

        return response.Orders;
    }

    private void LogRequest(WaveContext context, int number, AllocationRequestResult request)
    {
        if (request.Response is null)
        {
            _metrics.AllocationRequestSent(context.Customer, context.SameDay, null, TimeSpan.Zero);
            _logger.LogError(request.Failure,
                "Allocator {Allocator}'s {WaveKind} wave at {WaveTime} for owner {OwnerId}: request {RequestNumber} " +
                "({OrderCount} orders) got no response from Throughline",
                context.Allocator, context.Kind, context.Wave, context.OwnerId, number, request.OrderIds.Count);
            return;
        }

        var response = request.Response;
        var status = (int)response.StatusCode;
        _metrics.AllocationRequestSent(context.Customer, context.SameDay, status, response.Elapsed);

        switch (WaveRelease.Classify(response.StatusCode))
        {
            case WaveResult.Allocated:
                LogAllocated(context, number, request.OrderIds.Count, response);
                break;

            case WaveResult.Rejected:
                _logger.LogError(
                    "Allocator {Allocator}'s {WaveKind} wave at {WaveTime} for owner {OwnerId}: request {RequestNumber} " +
                    "({OrderCount} orders) was rejected with {StatusCode}; the simulator built a bad request",
                    context.Allocator, context.Kind, context.Wave, context.OwnerId, number, request.OrderIds.Count,
                    status);
                break;

            default:
                _logger.LogError(
                    "Allocator {Allocator}'s {WaveKind} wave at {WaveTime} for owner {OwnerId}: request {RequestNumber} " +
                    "({OrderCount} orders) got {StatusCode}",
                    context.Allocator, context.Kind, context.Wave, context.OwnerId, number, request.OrderIds.Count,
                    status);
                break;
        }
    }

    private void LogAllocated(WaveContext context, int number, int orderCount, AllocationResponse response)
    {
        var summary = WaveSummary.From(response.Result);

        foreach (var (outcome, count) in summary.Outcomes)
            _metrics.OrdersAllocated(context.Customer, outcome, count);

        _logger.LogInformation(
            "Allocator {Allocator}'s {WaveKind} wave at {WaveTime} for owner {OwnerId}: request {RequestNumber}, " +
            "{OrderCount} orders, {FullyAllocated} fully allocated, {PartiallyAllocated} partially allocated, " +
            "{NotAllocated} not allocated, {Failed} failed, {UnitsShort} units short, in {ElapsedMs:F0} ms",
            context.Allocator, context.Kind, context.Wave, context.OwnerId, number, orderCount,
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

    private void LogRelease(WaveContext context, WaveProgress progress)
    {
        if (progress.Finished)
        {
            _logger.LogInformation(
                "Allocator {Allocator} released the {WaveKind} wave at {WaveTime} for owner {OwnerId}: " +
                "{OrdersReleased} of {OrderCount} orders ({BackorderedCount} backordered) in {RequestsSent} requests, " +
                "finished",
                context.Allocator, context.Kind, context.Wave, context.OwnerId, progress.OrdersReleased,
                progress.Covered.Count, progress.BackorderedCount, progress.RequestsSent);
            return;
        }

        _logger.LogWarning(
            "Allocator {Allocator} stopped the {WaveKind} wave at {WaveTime} for owner {OwnerId} early: " +
            "{OrdersReleased} of {OrderCount} orders ({BackorderedCount} backordered) in {RequestsSent} requests; " +
            "the rest wait for the next wave",
            context.Allocator, context.Kind, context.Wave, context.OwnerId, progress.OrdersReleased,
            progress.Covered.Count, progress.BackorderedCount, progress.RequestsSent);
    }

    // what a wave has done so far, across all its batches
    private sealed class WaveProgress(int backorderedCount)
    {
        public int BackorderedCount { get; } = backorderedCount;

        /// <summary>
        ///     Every order the wave has sent, or tried to
        /// </summary>
        public HashSet<Guid> Covered { get; } = [];

        public int OrdersReleased { get; private set; }

        public int RequestsSent { get; set; }

        /// <summary>
        ///     Set once a read of the unreleased list has nothing new
        /// </summary>
        public bool Finished { get; set; }

        public void Add(IEnumerable<Guid> batch, int ordersReleased)
        {
            Covered.UnionWith(batch);
            OrdersReleased += ordersReleased;
        }
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
