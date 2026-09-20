using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.EntityFrameworkCore;

namespace Throughline.Common.Infrastructure;

public static class ModuleDbContextExtensions
{
    /// <summary>
    ///     The single connection string shared by every module's DbContext. All modules persist
    ///     to one physical database, segregated by schema (each context's <c>HasDefaultSchema</c>).
    /// </summary>
    private const string ConnectionStringName = "Throughline";

    /// <summary>
    ///     Registers <typeparamref name="TContext" /> against the shared database with the standard
    ///     Postgres + snake_case conventions and Wolverine transactional inbox/outbox integration.
    ///     The EF migrations-history table is placed in <paramref name="schema" />
    ///     so each module owns its own migration ledger rather
    ///     than sharing a single <c>public.__EFMigrationsHistory</c> across modules.
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services, IConfiguration configuration, string schema)
        where TContext : DbContext
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is missing or empty. " +
                $"Set ConnectionStrings:{ConnectionStringName} in configuration.");

        return services.AddDbContextWithWolverineIntegration<TContext>(o =>
            o.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, schema))
                .UseSnakeCaseNamingConvention());
    }
}