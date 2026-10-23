using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;

namespace Throughline.Simulator.Allocation;

/// <summary>
///     An outbound planner reading a customer's allocation worklists (<c>GET /inventory/orders/backordered</c> and
///     <c>/unreleased</c>) and releasing a wave (<c>POST /inventory/orders/allocate</c>). Defined here rather than
///     shared with Throughline, as an outside caller's would be.
/// </summary>
internal sealed class InventoryClient
{
    private const string OwnerIdHeader = "owner_id";

    private readonly HttpClient _http;

    public InventoryClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<WorklistResponse> GetWorklistAsync(
        int ownerId, Worklist worklist, int batchSize, CancellationToken cancellationToken)
    {
        var route = worklist == Worklist.Backordered ? "backordered" : "unreleased";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"inventory/orders/{route}?batchSize={batchSize.ToString(CultureInfo.InvariantCulture)}");
        request.Headers.Add(OwnerIdHeader, ownerId.ToString(CultureInfo.InvariantCulture));

        var started = Stopwatch.GetTimestamp();
        using var response = await _http.SendAsync(request, cancellationToken);
        var elapsed = Stopwatch.GetElapsedTime(started);

        IReadOnlyList<WorklistOrder>? orders = null;

        if (response.StatusCode == HttpStatusCode.OK)
            orders = await response.Content.ReadFromJsonAsync<List<WorklistOrder>>(cancellationToken);

        return new WorklistResponse(response.StatusCode, orders, elapsed);
    }

    public async Task<AllocationResponse> AllocateAsync(
        int ownerId, IReadOnlyList<Guid> orderIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(orderIds);

        using var request = new HttpRequestMessage(HttpMethod.Post, "inventory/orders/allocate");
        request.Content = JsonContent.Create(new AllocationRequest(orderIds));
        request.Headers.Add(OwnerIdHeader, ownerId.ToString(CultureInfo.InvariantCulture));

        var started = Stopwatch.GetTimestamp();
        using var response = await _http.SendAsync(request, cancellationToken);
        var elapsed = Stopwatch.GetElapsedTime(started);

        AllocationResult? result = null;

        if (response.StatusCode == HttpStatusCode.OK)
            result = await response.Content.ReadFromJsonAsync<AllocationResult>(cancellationToken);

        return new AllocationResponse(response.StatusCode, result, elapsed);
    }

    private sealed record AllocationRequest(IReadOnlyList<Guid> OrderIds);
}

internal enum Worklist
{
    Backordered,
    Unreleased
}

/// <param name="OrderCreatedOn">When Throughline accepted the order</param>
internal sealed record WorklistOrder(Guid OrderId, DateTimeOffset OrderCreatedOn);

internal sealed record WorklistResponse(
    HttpStatusCode StatusCode, IReadOnlyList<WorklistOrder>? Orders, TimeSpan Elapsed);

internal sealed record AllocationResult(IReadOnlyList<AllocatedOrderResult> Orders);

/// <param name="Status">
///     <c>fullyAllocated</c>, <c>partiallyAllocated</c>, <c>notAllocated</c> or <c>failed</c>
/// </param>
internal sealed record AllocatedOrderResult(
    Guid OrderId,
    string Status,
    IReadOnlyList<ShortageResult> Shortages,
    IReadOnlyList<AllocationErrorResult> Errors);

internal sealed record ShortageResult(string Sku, int QuantityOutstanding);

internal sealed record AllocationErrorResult(string Code, string Description);

/// <param name="Elapsed">Time for the whole call, including any retries</param>
internal sealed record AllocationResponse(HttpStatusCode StatusCode, AllocationResult? Result, TimeSpan Elapsed);
