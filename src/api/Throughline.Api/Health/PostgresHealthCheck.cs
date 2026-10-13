using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Throughline.Api.Health;

/// <summary>
///     Healthy when a query round trip to Postgres succeeds. A failure throws: the health check
///     service reports it as unhealthy and logs the exception, so no detail is returned from here.
/// </summary>
internal sealed class PostgresHealthCheck : IHealthCheck
{
    // A probe needs its answer sooner than the connect and command timeouts requests run with
    private const int TimeoutSeconds = 3;

    private readonly string _connectionString;

    public PostgresHealthCheck(string connectionString)
    {
        _connectionString = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Timeout = TimeoutSeconds,
            CommandTimeout = TimeoutSeconds
        }.ConnectionString;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand("SELECT 1", connection);
        await command.ExecuteScalarAsync(cancellationToken);

        return HealthCheckResult.Healthy();
    }
}
