using System.Diagnostics;
using Azure.Identity;
using Microsoft.Extensions.Http.Resilience;
using Throughline.Simulator.Configuration;

namespace Throughline.Simulator.Oms;

/// <summary>
///     One named <see cref="HttpClient" /> per customer, each with its own credentials and retry profile
/// </summary>
internal static class OmsHttpClients
{
    public static string NameFor(string customer) => $"oms:{customer}";

    public static IServiceCollection AddOmsHttpClient(
        this IServiceCollection services, string customer, CustomerProfile profile, SimulatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(options);

        // Created on first use, after the options have been validated, and kept for the process's
        // life so the cached token survives the HTTP handler being recycled
        var tokens = new Lazy<AccessTokenSource>(() => new AccessTokenSource(
            new ClientSecretCredential(options.TenantId, profile.OmsClientId, profile.OmsClientSecret),
            options.ApiScope,
            TimeProvider.System));

        var retry = profile.Retry;
        var attemptTimeout = TimeSpan.FromSeconds(retry.AttemptTimeoutSeconds);

        // every attempt timing out plus the longest exponential backoff, doubled for jitter
        var totalTimeout = attemptTimeout * (retry.MaxRetryAttempts + 1) +
                           TimeSpan.FromSeconds(retry.BaseDelaySeconds * Math.Pow(2, retry.MaxRetryAttempts) * 2);

        services.AddHttpClient(NameFor(customer), client =>
            {
                // trailing slash so relative paths like "orders" resolve beneath it
                client.BaseAddress = new Uri(options.ThroughlineBaseUrl!.ToString().TrimEnd('/') + "/");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                // An outside OMS doesn't share trace context with the WMS: no traceparent header
                ActivityHeadersPropagator = DistributedContextPropagator.CreateNoOutputPropagator(),
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            })
            .AddHttpMessageHandler(() => new BearerTokenHandler(tokens.Value))

            // Retries timeouts, 408, 429 and 5xx with exponential backoff and jitter, never a 400.
            // A retry resends the same 940, as a real client would.
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
