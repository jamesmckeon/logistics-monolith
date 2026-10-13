using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Throughline.Api.Health;

internal static class HealthCheckExtensions
{
    public const string ReadinessRoute = "/health";
    public const string LivenessRoute = "/alive";

    private const string PostgresCheckName = "postgres";

    public static IServiceCollection AddThroughlineHealthChecks(
        this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddHealthChecks()
            .AddCheck(PostgresCheckName, new PostgresHealthCheck(connectionString));

        return services;
    }

    public static IEndpointRouteBuilder MapThroughlineHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // Readiness runs every registered check
        endpoints.MapHealthChecks(ReadinessRoute).AllowAnonymous();

        // Liveness runs none: a Postgres outage must not get a working process restarted
        endpoints.MapHealthChecks(LivenessRoute, new HealthCheckOptions { Predicate = _ => false })
            .AllowAnonymous();

        return endpoints;
    }
}