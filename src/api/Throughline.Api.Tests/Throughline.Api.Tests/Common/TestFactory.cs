using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Throughline.Api.Tests.Common.Logging;

namespace Throughline.Api.Tests.Common;

/// <summary>
///     Initializes the postgres and Liquibase containers and logging, and applies the app settings it is given
/// </summary>
internal sealed class TestFactory : WebApplicationFactory<Program>

{
    // Pulling the image (cold) and booting the container can take a while on a fresh
    // CI runner, so startup gets a generous budget of its own.
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(5);

    // Fail fast instead of hanging when Postgres is unresponsive mid-test.
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(15);

    private const string DbNetworkAlias = "postgres";
    private const string DbName = "throughline";
    private const string DbUser = "postgres";
    private const string DbPassword = "postgres";

    // Wolverine no longer builds its own tables, so its schema is built before the host starts.
    private const string WolverineSchema = "wolverine";

    // Changeset identity includes the changelog path relative to this directory, so it matches
    // the mount point every other environment uses (see db/Dockerfile).
    private const string ChangelogMountPath = "/liquibase/changelog";

    private static readonly string DbDirectory =
        Path.Combine(CommonDirectoryPath.GetSolutionDirectory().DirectoryPath, "db");

    // Built once per test run and shared by every class; Docker's layer cache makes rebuilds cheap.
    private static readonly IFutureDockerImage LiquibaseImage = new ImageFromDockerfileBuilder()
        .WithDockerfileDirectory(DbDirectory)
        .WithName("throughline-liquibase:tests")
        .Build();

    private static readonly Lazy<Task> LiquibaseImageCreated = new(() => LiquibaseImage.CreateAsync());

    private readonly INetwork _network = new NetworkBuilder()
        .WithName(Guid.NewGuid().ToString())
        .Build();

    private readonly PostgreSqlContainer _dbContainer;
    private readonly IContainer _liquibaseContainer;

    private readonly IReadOnlyDictionary<string, string> _appSettings;

    public TestFactory(IReadOnlyDictionary<string, string> appSettings)
    {
        _appSettings = appSettings;

        _dbContainer = new PostgreSqlBuilder("postgres:15-alpine")
            .WithName(Guid.NewGuid().ToString())
            .WithDatabase(DbName)
            .WithUsername(DbUser)
            .WithPassword(DbPassword)
            .WithNetwork(_network)
            .WithNetworkAliases(DbNetworkAlias)
            .Build();

        // Kept running so each changelog is applied with an exec, which returns its exit code and output.
        _liquibaseContainer = new ContainerBuilder(LiquibaseImage)
            .WithName(Guid.NewGuid().ToString())
            .WithEntrypoint("sleep", "infinity")
            .WithWorkingDirectory(ChangelogMountPath)
            .WithBindMount(Path.Combine(DbDirectory, "changelog"), ChangelogMountPath, AccessMode.ReadOnly)
            .WithNetwork(_network)
            .Build();
    }

    public async Task InitializeAsync()
    {
        using var cts = new CancellationTokenSource(StartupTimeout);
        await LiquibaseImageCreated.Value.WaitAsync(cts.Token);
        await _network.CreateAsync(cts.Token);
        await Task.WhenAll(_dbContainer.StartAsync(cts.Token), _liquibaseContainer.StartAsync(cts.Token));

        var schemas = await _dbContainer.ExecScriptAsync(
            await File.ReadAllTextAsync(Path.Combine(DbDirectory, "create-schemas.sql"), cts.Token), cts.Token);
        if (schemas.ExitCode != 0)
        {
            throw new InvalidOperationException($"create-schemas.sql failed:{Environment.NewLine}{schemas.Stderr}");
        }

        await ApplyChangelogAsync(WolverineSchema, cts.Token);
    }

    /// <summary>
    ///     Applies the changelog of one module's DbContext, found by the context's schema. Every module's
    ///     context is registered in the host, so a test class builds only the schemas it uses.
    /// </summary>
    public async Task MigrateAsync<TContext>()
        where TContext : DbContext
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        var schema = dbContext.Model.GetDefaultSchema()
                     ?? throw new InvalidOperationException($"{typeof(TContext).Name} has no default schema.");

        await ApplyChangelogAsync(schema, CancellationToken.None);
    }

    private async Task ApplyChangelogAsync(string schema, CancellationToken ct)
    {
        var result = await _liquibaseContainer.ExecAsync(
        [
            "liquibase",
            $"--url=jdbc:postgresql://{DbNetworkAlias}:5432/{DbName}",
            $"--username={DbUser}",
            $"--password={DbPassword}",
            $"--changelog-file={schema}/changelog.yaml",
            $"--liquibase-schema-name={schema}",
            "--show-banner=false",
            "update"
        ], ct);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"liquibase update failed for {schema}:{Environment.NewLine}{result.Stdout}{result.Stderr}");
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Timeout = connection establishment; Command Timeout = per-query wait. Both in seconds.
        var seconds = (int)QueryTimeout.TotalSeconds;
        var connectionString =
            $"{_dbContainer.GetConnectionString()};Timeout={seconds};Command Timeout={seconds}";
        builder.UseSetting("ConnectionStrings:Throughline", connectionString);

        foreach (var setting in _appSettings)
        {
            builder.UseSetting(setting.Key, setting.Value);
        }

        // Requests run on the test's ExecutionContext, so NUnitLoggerProvider's TestContext.Progress
        // writes from request handling (e.g. GlobalExceptionHandler) are attributed to the test, not dropped.
        builder.UseTestServer(o => o.PreserveExecutionContext = true);

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddProvider(new NUnitLoggerProvider());
            logging.SetMinimumLevel(LogLevel.Information);
        });
    }

    public new async Task DisposeAsync()
    {
        // DisposeAsync (not StopAsync) is safe even when startup failed before the
        // containers were created, so teardown never masks the real setup error.
        await _liquibaseContainer.DisposeAsync();
        await _dbContainer.DisposeAsync();
        await _network.DisposeAsync();
    }
}
