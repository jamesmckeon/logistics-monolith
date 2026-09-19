using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.EntityFrameworkCore;

namespace Throughline.Common.Infrastructure;

public static class ModuleDbContextExtensions
{
    /// <summary>
    ///     The single connection string shared by every module's DbContext. All modules persist
    ///     to one physical database, segregated by schema (each context's <c>HasDefaultSchema</c>),
    ///     so the key lives here once rather than being copied into every module's composition root.
    /// </summary>
    public const string ConnectionStringName = "Throughline";

    /// <summary>
    ///     Registers <typeparamref name="TContext"/> against the shared database with the standard
    ///     Postgres + snake_case conventions and Wolverine transactional inbox/outbox integration.
    ///     A module owns its context type and schema; this owns how that context is built, so the
    ///     provider and EF policy are defined in one place instead of drifting across modules.
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services, IConfiguration configuration)
        where TContext : DbContext
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is missing or empty. " +
                $"Set ConnectionStrings:{ConnectionStringName} in configuration.");

        return services.AddDbContextWithWolverineIntegration<TContext>(o =>
            o.UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());
    }
}
