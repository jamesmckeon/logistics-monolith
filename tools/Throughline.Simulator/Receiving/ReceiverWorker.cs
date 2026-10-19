using System.Net;
using Microsoft.Extensions.Options;
using Throughline.Simulator.Configuration;
using Throughline.Simulator.Http;
using Throughline.Simulator.Identity;
using Throughline.Simulator.Ledger;
using Throughline.Simulator.Scheduling;
using Throughline.Simulator.Telemetry;

namespace Throughline.Simulator.Receiving;

/// <summary>
///     The facility's receivers. While the dock is staffed, each takes the trailer that's waited
///     longest, receives it into Throughline as the signed-in user, and then takes the next.
/// </summary>
internal sealed class ReceiverWorker : BackgroundService
{
    private static readonly TimeSpan RetryAfterSignInFailure = TimeSpan.FromMinutes(15);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILedger _ledger;
    private readonly ILogger<ReceiverWorker> _logger;
    private readonly SimulatorMetrics _metrics;
    private readonly SimulatorOptions _options;
    private readonly TimeProvider _time;
    private readonly TokenSources _tokens;

    public ReceiverWorker(
        IOptions<SimulatorOptions> options,
        IHttpClientFactory httpClientFactory,
        TokenSources tokens,
        ILedger ledger,
        SimulatorMetrics metrics,
        TimeProvider time,
        ILogger<ReceiverWorker> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _tokens = tokens;
        _ledger = ledger;
        _metrics = metrics;
        _time = time;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return Task.CompletedTask;

        return Task.WhenAll(_options.Facility.Receivers.Select(r => RunReceiverAsync(r.Username, stoppingToken)));
    }

    private async Task RunReceiverAsync(string username, CancellationToken stoppingToken)
    {
        using var _ = _logger.BeginScope(new Dictionary<string, object> { ["Receiver"] = username });

        var dock = DockCalendar.From(_options.Facility);
        var random = new Random();
        var client = new ReceivingClient(_httpClientFactory.CreateClient(ThroughlineHttpClients.ForStaff(username)));
        var operatorId = await SignInAsync(username, stoppingToken);

        _logger.LogInformation("Receiver {Receiver} started as operator {OperatorId}", username, operatorId);

        while (!stoppingToken.IsCancellationRequested)
        {
            // time to unload a trailer, or to check the dock again when there was nothing to do
            var minutes = _options.Facility.MinutesPerTrailer * (0.5 + random.NextDouble());

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(minutes), _time, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var now = _time.GetUtcNow();

            if (!dock.IsOpen(now))
                continue;

            try
            {
                var shipment = await _ledger.ClaimDueShipmentAsync(username, now, stoppingToken);

                if (shipment is not null)
                    await ReceiveAsync(username, operatorId, client, shipment, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Receiver {Receiver} couldn't take the next trailer", username);
            }
        }
    }

    // The operator id Throughline records is the signed-in user's own Entra object id
    private async Task<Guid> SignInAsync(string username, CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                var token = await _tokens.ForStaff(username).GetTokenAsync(false, stoppingToken);
                return JwtClaims.ObjectId(token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Receiver {Receiver} couldn't sign in; retrying in {Delay}",
                    username, RetryAfterSignInFailure);
                await Task.Delay(RetryAfterSignInFailure, _time, stoppingToken);
            }
        }
    }

    private async Task ReceiveAsync(
        string username, Guid operatorId, ReceivingClient client, InboundShipment shipment,
        CancellationToken stoppingToken)
    {
        ReceiptResponse response;

        try
        {
            response = await client.ReceiveAsync(shipment, operatorId, stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // timeouts and connection failures that outlasted the retries: try the trailer again later
            await _ledger.ReleaseAsync(shipment.ShipmentId, stoppingToken);
            _metrics.ReceiptSent(username, null, shipment.Pallets.Count, TimeSpan.Zero);
            _logger.LogError(ex, "Trailer {TrailerNumber} ({DeliveryReference}) got no response from Throughline",
                shipment.TrailerNumber, shipment.DeliveryReference);
            return;
        }

        var status = (int)response.StatusCode;
        _metrics.ReceiptSent(username, status, shipment.Pallets.Count, response.Elapsed);

        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                await _ledger.MarkReceivedAsync(shipment.ShipmentId, status, _time.GetUtcNow(), stoppingToken);
                LogReceived(shipment, response);
                break;

            // our own sign-in or role problem, not the trailer's: keep it for when it's fixed
            case HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden:
            case >= HttpStatusCode.InternalServerError:
                await _ledger.ReleaseAsync(shipment.ShipmentId, stoppingToken);
                _logger.LogError(
                    "Trailer {TrailerNumber} ({DeliveryReference}) for owner {OwnerId} got {StatusCode}; " +
                    "it will be tried again", shipment.TrailerNumber, shipment.DeliveryReference,
                    shipment.OwnerId, status);
                break;

            default:
                await _ledger.MarkRejectedAsync(shipment.ShipmentId, status, _time.GetUtcNow(), stoppingToken);
                _logger.LogWarning(
                    "Trailer {TrailerNumber} ({DeliveryReference}) for owner {OwnerId} was rejected with {StatusCode}",
                    shipment.TrailerNumber, shipment.DeliveryReference, shipment.OwnerId, status);
                break;
        }
    }

    private void LogReceived(InboundShipment shipment, ReceiptResponse response)
    {
        var outcomes = response.Result?.Pallets.GroupBy(p => p.Outcome).ToDictionary(g => g.Key, g => g.Count())
                       ?? new Dictionary<string, int>();

        _logger.LogInformation(
            "Received trailer {TrailerNumber} ({DeliveryReference}) for owner {OwnerId} as receipt {ReceiptNumber}: " +
            "{PalletCount} pallets, {Available} available, {OnHold} on hold, {Exceptions} exceptions, in {ElapsedMs:F0} ms",
            shipment.TrailerNumber, shipment.DeliveryReference, shipment.OwnerId, response.Result?.ReceiptNumber,
            shipment.Pallets.Count,
            outcomes.GetValueOrDefault("received_available"),
            outcomes.GetValueOrDefault("received_on_hold"),
            outcomes.GetValueOrDefault("receiving_exception"),
            response.Elapsed.TotalMilliseconds);
    }
}
