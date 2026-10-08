using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Throughline.Api.Tests.Common;

/// <summary>
///     TestFactory for a single DbContext test class.  Allows for overriding app configuration
/// </summary>
internal class TestFactory<TContext>(IReadOnlyDictionary<string, string>? ConfigurationSettings) : TestFactory
    where TContext : DbContext
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:15-alpine")
        .WithName(Guid.NewGuid().ToString())
        .Build();

    protected override void OnWebHostConfiguring(IWebHostBuilder builder)
    {
        if (ConfigurationSettings != null)
        {
            foreach (var setting in ConfigurationSettings)
            {
                builder.UseSetting(setting.Key, setting.Value);
            }
        }
    }

    internal async Task MigrateDbAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        await dbContext.Database.MigrateAsync();
    }
}