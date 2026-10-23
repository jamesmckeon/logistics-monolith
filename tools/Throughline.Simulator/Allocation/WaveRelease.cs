using System.Net;

namespace Throughline.Simulator.Allocation;

/// <summary>
///     Allocates a wave's orders the way a planner would: consecutive requests of at most
///     <see cref="MaxOrdersPerRequest" /> orders, one at a time, stopping at the first request that isn't allocated so
///     the rest wait for the next wave
/// </summary>
internal sealed class WaveRelease
{
    /// <summary>
    ///     The most orders a planner selects for one allocation request, and the allocate endpoint's cap (#44)
    /// </summary>
    public const int MaxOrdersPerRequest = 50;

    private readonly IInventoryClient _client;

    public WaveRelease(IInventoryClient client)
    {
        _client = client;
    }

    /// <summary>
    ///     <paramref name="orderIds" /> as consecutive requests of at most <paramref name="maxOrdersPerRequest" />,
    ///     in the same order
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<Guid>> SplitIntoRequests(
        IReadOnlyList<Guid> orderIds, int maxOrdersPerRequest)
    {
        ArgumentNullException.ThrowIfNull(orderIds);

        return orderIds.Chunk(maxOrdersPerRequest).Select(c => (IReadOnlyList<Guid>)c).ToList();
    }

    public static WaveResult Classify(HttpStatusCode status) => status switch
    {
        HttpStatusCode.OK => WaveResult.Allocated,

        // the simulator built a bad request; sending it again won't help
        HttpStatusCode.BadRequest => WaveResult.Rejected,

        // sign-in, role or server problems: the orders stay on the worklists for the next wave
        _ => WaveResult.LeftForNextWave
    };

    public async Task<WaveReleaseResult> ReleaseAsync(
        int ownerId, IReadOnlyList<Guid> orderIds, CancellationToken cancellationToken)
    {
        var requests = SplitIntoRequests(orderIds, MaxOrdersPerRequest);
        var sent = new List<AllocationRequestResult>();

        foreach (var request in requests)
        {
            AllocationResponse response;

            try
            {
                response = await _client.AllocateAsync(ownerId, request, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // timeouts and connection failures that outlasted the retries
                sent.Add(new AllocationRequestResult(request, null, ex));
                break;
            }

            sent.Add(new AllocationRequestResult(request, response, null));

            if (Classify(response.StatusCode) != WaveResult.Allocated)
                break;
        }

        return new WaveReleaseResult(requests.Count, sent);
    }
}

internal enum WaveResult
{
    Allocated,
    Rejected,
    LeftForNextWave
}

/// <param name="Response">Throughline's response, or null when none arrived</param>
/// <param name="Failure">Why no response arrived</param>
internal sealed record AllocationRequestResult(
    IReadOnlyList<Guid> OrderIds, AllocationResponse? Response, Exception? Failure)
{
    public bool Allocated => Response is not null && WaveRelease.Classify(Response.StatusCode) == WaveResult.Allocated;
}

/// <param name="RequestsPlanned">Requests the wave's orders were split into</param>
/// <param name="Requests">The requests sent, in order; fewer than planned when the wave stopped early</param>
internal sealed record WaveReleaseResult(int RequestsPlanned, IReadOnlyList<AllocationRequestResult> Requests)
{
    public bool Finished => Requests.Count == RequestsPlanned && Requests.All(r => r.Allocated);

    /// <summary>
    ///     Orders in requests Throughline allocated
    /// </summary>
    public int OrdersReleased => Requests.Where(r => r.Allocated).Sum(r => r.OrderIds.Count);
}
