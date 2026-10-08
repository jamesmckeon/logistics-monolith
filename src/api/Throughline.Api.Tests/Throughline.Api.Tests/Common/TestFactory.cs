using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace Throughline.Api.Tests.Common;

/// <summary>
///     Initializes postgres container and logging
/// </summary>
internal class TestFactory : WebApplicationFactory<Program>

{
    // Pulling the image (cold) and booting the container can take a while on a fresh
    // CI runner, so startup gets a generous budget of its own.
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(5);

    // Fail fast instead of hanging when Postgres is unresponsive mid-test.
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(15);

    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:15-alpine")
        .WithName(Guid.NewGuid().ToString())
        .Build();

    public async Task InitializeAsync()
    {
        using var cts = new CancellationTokenSource(StartupTimeout);
        await _dbContainer.StartAsync(cts.Token);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Timeout = connection establishment; Command Timeout = per-query wait. Both in seconds.
        var seconds = (int)QueryTimeout.TotalSeconds;
        var connectionString =
            $"{_dbContainer.GetConnectionString()};Timeout={seconds};Command Timeout={seconds}";
        builder.UseSetting("ConnectionStrings:Throughline", connectionString);

        // Requests run on the test's ExecutionContext, so NUnitLoggerProvider's TestContext.Progress
        // writes from request handling (e.g. GlobalExceptionHandler) are attributed to the test, not dropped.
        builder.UseTestServer(o => o.PreserveExecutionContext = true);

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddProvider(new NUnitLoggerProvider());
            logging.SetMinimumLevel(LogLevel.Information);
        });

        OnWebHostConfiguring(builder);
    }

    protected virtual void OnWebHostConfiguring(IWebHostBuilder builder)
    {
    }

    public new async Task DisposeAsync()
    {
        // DisposeAsync (not StopAsync) is safe even when startup failed before the
        // container was created, so teardown never masks the real setup error.
        await _dbContainer.DisposeAsync();
    }
}