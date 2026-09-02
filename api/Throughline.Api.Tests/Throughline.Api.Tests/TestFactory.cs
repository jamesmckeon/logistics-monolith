using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Throughline.Modules.Ordering.Infrastructure.Orders;

namespace Throughline.Api.Tests;

internal sealed class TestFactory : WebApplicationFactory<Program>
{
    // Fail fast instead of hanging when Docker or Postgres is unresponsive.
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(15);

    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:15-alpine")
        .WithName(Guid.NewGuid().ToString())
        .Build();

    public async Task InitializeAsync()
    {
        // Bounds container startup (image pull / readiness / Ryuk), which happens before any
        // SQL and so is not covered by the connection-string timeouts below.
        using var cts = new CancellationTokenSource(ResponseTimeout);
        await _dbContainer.StartAsync(cts.Token);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Timeout = connection establishment; Command Timeout = per-query wait. Both in seconds.
        var seconds = (int)ResponseTimeout.TotalSeconds;
        var connectionString =
            $"{_dbContainer.GetConnectionString()};Timeout={seconds};Command Timeout={seconds}";
        builder.UseSetting("ConnectionStrings:Throughline", connectionString);
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }

    public async Task ApplyMigrationsAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}