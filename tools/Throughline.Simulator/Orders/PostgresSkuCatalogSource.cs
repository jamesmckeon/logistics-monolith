using Microsoft.Extensions.Options;
using Npgsql;
using Throughline.Simulator.Configuration;

namespace Throughline.Simulator.Orders;

/// <summary>
///     Where a customer's SKUs come from
/// </summary>
internal interface ISkuCatalogSource
{
    /// <summary>
    ///     The owner's SKU codes, most popular first
    /// </summary>
    Task<IReadOnlyList<string>> GetSkusAsync(int ownerId, CancellationToken cancellationToken);
}

/// <summary>
///     Reads a customer's SKUs from Throughline's <c>inventory.skus</c>, signed in as a role that may
///     only select from that table. This is the one place the simulator touches Throughline's
///     database: catalog setup, not simulated traffic (docs/design/simulator.md, decision 11).
/// </summary>
internal sealed class PostgresSkuCatalogSource : ISkuCatalogSource, IAsyncDisposable
{
    // Throughline has no sales history to rank by, so popularity is a stable pseudo-random order:
    // the same SKUs are "hot" on every load and every restart
    private const string Sql = "SELECT code FROM inventory.skus WHERE owner_id = $1 ORDER BY md5(code)";

    // created on first use, after the options have been validated
    private readonly Lazy<NpgsqlDataSource> _dataSource;

    public PostgresSkuCatalogSource(IOptions<SimulatorOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _dataSource = new Lazy<NpgsqlDataSource>(() => NpgsqlDataSource.Create(options.Value.CatalogConnectionString));
    }

    public async ValueTask DisposeAsync()
    {
        if (_dataSource.IsValueCreated)
            await _dataSource.Value.DisposeAsync();
    }

    public async Task<IReadOnlyList<string>> GetSkusAsync(int ownerId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.Value.CreateCommand(Sql);
        command.Parameters.Add(new NpgsqlParameter { Value = ownerId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var skus = new List<string>();

        while (await reader.ReadAsync(cancellationToken))
            skus.Add(reader.GetString(0));

        return skus;
    }
}
