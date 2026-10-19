using System.Diagnostics;
using Microsoft.Extensions.Http.Resilience;
using Throughline.Simulator.Configuration;
using Throughline.Simulator.Identity;
using Throughline.Simulator.Oms;

namespace Throughline.Simulator.Http;

/// <summary>
///     Named <see cref="HttpClient" />s for calling Throughline, one per caller, each with its own token
///     and retry profile
/// </summary>
internal static class ThroughlineHttpClients
{
    public static string ForCustomer(string customer) => $"oms:{customer}";

    public static string ForStaff(string username) => $"staff:{username}";

    public static IServiceCollection AddThroughlineClient(
        this IServiceCollection services,
        string name,
        Uri throughlineBaseUrl,
        RetryProfile retry,
        Func<TokenSources, AccessTokenSource> tokenSource)
    {
        ArgumentNullException.ThrowIfNull(throughlineBaseUrl);
        ArgumentNullException.ThrowIfNull(retry);
        ArgumentNullException.ThrowIfNull(tokenSource);

        var attemptTimeout = TimeSpan.FromSeconds(retry.AttemptTimeoutSeconds);

        // every attempt timing out plus the longest exponential backoff, doubled for jitter
        var totalTimeout = attemptTimeout * (retry.MaxRetryAttempts + 1) +
                           TimeSpan.FromSeconds(retry.BaseDelaySeconds * Math.Pow(2, retry.MaxRetryAttempts) * 2);

        // trailing slash so relative paths like "orders" resolve beneath it
        var baseAddress = new Uri(throughlineBaseUrl.ToString().TrimEnd('/') + "/");

        services.AddHttpClient(name, client => client.BaseAddress = baseAddress)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                // the simulator plays outside callers, which don't share trace context with the WMS
                ActivityHeadersPropagator = DistributedContextPropagator.CreateNoOutputPropagator(),
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            })
            .AddHttpMessageHandler(provider => new BearerTokenHandler(tokenSource(provider.GetRequiredService<TokenSources>())))

            // Retries timeouts, 408, 429 and 5xx with exponential backoff and jitter, never a 400.
            // A retry resends the same request, as a real client would.
            .AddStandardResilienceHandler(resilience =>
            {
                resilience.Retry.MaxRetryAttempts = retry.MaxRetryAttempts;
                resilience.Retry.Delay = TimeSpan.FromSeconds(retry.BaseDelaySeconds);
                resilience.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                resilience.Retry.UseJitter = true;
                resilience.AttemptTimeout.Timeout = attemptTimeout;
                resilience.TotalRequestTimeout.Timeout = totalTimeout;

                // must be at least twice the attempt timeout
                resilience.CircuitBreaker.SamplingDuration =
                    TimeSpan.FromTicks(Math.Max(TimeSpan.FromSeconds(30).Ticks, attemptTimeout.Ticks * 2));
            });

        return services;
    }
}
