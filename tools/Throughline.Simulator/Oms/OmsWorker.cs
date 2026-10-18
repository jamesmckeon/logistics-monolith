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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OmsWorker> _logger;
    private readonly SimulatorMetrics _metrics;
    private readonly SimulatorOptions _options;
    private readonly TimeProvider _time;

    public OmsWorker(
        IOptions<SimulatorOptions> options,
        IHttpClientFactory httpClientFactory,
        SimulatorMetrics metrics,
        TimeProvider time,
        ILogger<OmsWorker> logger)
    {
        _options = options.Value;
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
        var random = new Random();
        var schedule = ArrivalSchedule.From(profile);
        var generator = new OrderGenerator(profile, SkuCatalog.From(profile.Catalog), random, _time);
        var client = new OmsClient(_httpClientFactory.CreateClient(OmsHttpClients.NameFor(customer)));

        using var _ = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Customer"] = customer,
            ["OwnerId"] = profile.OwnerId
        });

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

            await SendAsync(customer, profile.OwnerId, generator, client, stoppingToken);
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
