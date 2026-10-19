using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Throughline.Simulator.Orders;

namespace Throughline.Simulator.Oms;

/// <summary>
///     A customer's OMS posting 940s to Throughline. Tokens, retries and the suppressed trace headers
///     are set up on the underlying <see cref="HttpClient" /> (see <see cref="Http.ThroughlineHttpClients" />).
/// </summary>
internal sealed class OmsClient
{
    private const string OwnerIdHeader = "owner_id";

    private readonly HttpClient _http;

    public OmsClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<OmsResponse> SendOrderAsync(
        int ownerId, OrderRequest order, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(order);

        using var request = new HttpRequestMessage(HttpMethod.Post, "orders");
        request.Content = JsonContent.Create(order);
        request.Headers.Add(OwnerIdHeader, ownerId.ToString(CultureInfo.InvariantCulture));

        var started = Stopwatch.GetTimestamp();
        using var response = await _http.SendAsync(request, cancellationToken);
        var elapsed = Stopwatch.GetElapsedTime(started);

        Guid? orderId = null;

        if (response.IsSuccessStatusCode)
        {
            var accepted = await response.Content.ReadFromJsonAsync<OrderAccepted>(cancellationToken);
            orderId = accepted?.OrderId;
        }

        return new OmsResponse(response.StatusCode, orderId, elapsed);
    }

    private sealed record OrderAccepted(Guid OrderId);
}

/// <param name="Elapsed">Time for the whole call, including any retries</param>
internal sealed record OmsResponse(HttpStatusCode StatusCode, Guid? OrderId, TimeSpan Elapsed);
