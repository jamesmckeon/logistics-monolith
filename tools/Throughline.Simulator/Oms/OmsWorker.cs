using Microsoft.Extensions.Options;
using Throughline.Simulator.Configuration;
using Throughline.Simulator.Orders;
using Throughline.Simulator.Scheduling;
using Throughline.Simulator.Telemetry;

namespace Throughline.Simulator.Oms;

/// <summary>
///     Runs every enabled customer's OMS: each waits for its next order according to its profile,
///     sends it, and logs whether Throughline responded as expected
/// </summary>
internal sealed class OmsWorker : BackgroundService
{
    private static readonly TimeSpan CatalogRefreshInterval = TimeSpan.FromDays(1);

    private readonly ISkuCatalogSource _catalogSource;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OmsWorker> _logger;
    private readonly SimulatorMetrics _metrics;
    private readonly SimulatorOptions _options;
    private readonly TimeProvider _time;

    public OmsWorker(
        IOptions<SimulatorOptions> options,
        ISkuCatalogSource catalogSource,
        IHttpClientFactory httpClientFactory,
        SimulatorMetrics metrics,
        TimeProvider time,
        ILogger<OmsWorker> logger)
    {
        _options = options.Value;
        _catalogSource = catalogSource;
        _httpClientFactory = httpClientFactory;
        _metrics = metrics;
        _time = time;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("The simulator is disabled (Simulator:Enabled is false); no traffic will be sent");
            return Task.CompletedTask;
        }

        var customers = _options.Customers.Where(c => c.Value.Enabled).ToList();

        return Task.WhenAll(customers.Select(c => RunCustomerAsync(c.Key, c.Value, stoppingToken)));
    }

    private async Task RunCustomerAsync(string customer, CustomerProfile profile, CancellationToken stoppingToken)
    {
        using var _ = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Customer"] = customer,
            ["OwnerId"] = profile.OwnerId
        });

        // a catalog that can't be loaded at startup stops the simulator; there's nothing to order
        var random = new Random();
        var schedule = ArrivalSchedule.From(profile);
        var generator = new OrderGenerator(
            profile, await LoadCatalogAsync(customer, profile, stoppingToken), random, _time);
        var catalogLoadedAt = _time.GetUtcNow();
        var client = new OmsClient(_httpClientFactory.CreateClient(OmsHttpClients.NameFor(customer)));

        _logger.LogInformation(
            "OMS for {Customer} started: {OrdersPerDay} orders on an average day, {TimeZone}",
            customer, profile.OrdersPerDay, profile.TimeZone);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = schedule.NextDelay(_time.GetUtcNow(), random);
            _logger.LogDebug("Next order for {Customer} in {Delay}", customer, delay);

            try
            {
                await Task.Delay(delay, _time, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (_time.GetUtcNow() - catalogLoadedAt >= CatalogRefreshInterval)
            {
                catalogLoadedAt = _time.GetUtcNow();
                await RefreshCatalogAsync(customer, profile, generator, stoppingToken);
            }

            await SendAsync(customer, profile.OwnerId, generator, client, stoppingToken);
        }
    }

    private async Task<SkuCatalog> LoadCatalogAsync(
        string customer, CustomerProfile profile, CancellationToken cancellationToken)
    {
        var skus = await _catalogSource.GetSkusAsync(profile.OwnerId, cancellationToken);

        // an order can't repeat a SKU, so a typical large order needs at least this many
        if (skus.Count < profile.LinesPerOrder.P95)
            throw new InvalidOperationException(
                $"Throughline has {skus.Count} SKUs for owner {profile.OwnerId} ({customer}); " +
                $"the simulator needs at least {profile.LinesPerOrder.P95}");

        _logger.LogInformation("Loaded {SkuCount} SKUs for {Customer} from Throughline", skus.Count, customer);

        return SkuCatalog.From(skus, profile.Catalog);
    }

    private async Task RefreshCatalogAsync(
        string customer, CustomerProfile profile, OrderGenerator generator, CancellationToken cancellationToken)
    {
        try
        {
            generator.UseCatalog(await LoadCatalogAsync(customer, profile, cancellationToken));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // keep ordering from yesterday's catalog rather than stopping
            _logger.LogWarning(ex, "Couldn't reload the SKUs for {Customer}; keeping the ones already loaded",
                customer);
        }
    }

    private async Task SendAsync(
        string customer, int ownerId, OrderGenerator generator, OmsClient client, CancellationToken stoppingToken)
    {
        var order = generator.Next();
        var request = order.Request;

        try
        {
            var response = await client.SendOrderAsync(ownerId, request, stoppingToken);
            var asExpected = response.StatusCode == order.ExpectedStatus;

            if (response.StatusCode == System.Net.HttpStatusCode.Created)
                generator.RecordAccepted(request);

            _metrics.OrderSent(customer, order.Kind, (int)response.StatusCode, asExpected, response.Elapsed);

            _logger.Log(
                asExpected ? LogLevel.Information : LogLevel.Warning,
                "{Kind} 940 ref {ReferenceNumber}, PO {PurchaseOrderNumber}, {LineCount} lines: " +
                "{StatusCode} (expected {ExpectedStatus}) in {ElapsedMs:F0} ms, Throughline order {OrderId}",
                order.Kind, request.ReferenceNumber, request.PurchaseOrderNumber, request.Items.Count,
                (int)response.StatusCode, (int)order.ExpectedStatus, response.Elapsed.TotalMilliseconds,
                response.OrderId);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutting down
        }
        catch (Exception ex)
        {
            // timeouts and connection failures that outlasted the retries; keep the loop running
            _metrics.OrderSent(customer, order.Kind, null, false, TimeSpan.Zero);
            _logger.LogError(ex,
                "{Kind} 940 ref {ReferenceNumber}, PO {PurchaseOrderNumber} got no response from Throughline",
                order.Kind, request.ReferenceNumber, request.PurchaseOrderNumber);
        }
    }
}
