using Microsoft.Extensions.Options;
using Npgsql;
using Testcontainers.PostgreSql;
using Throughline.Simulator.Configuration;
using Throughline.Simulator.Orders;

namespace Throughline.Simulator.Tests.Orders;

[Category("Integration")]
public sealed class PostgresSkuCatalogSourceTests
{
    // Only the columns the simulator reads. The simulator doesn't reference Throughline's code, so
    // it can't run Inventory's migrations; a change to inventory.skus surfaces when it runs.
    private const string Schema = """
                                  CREATE SCHEMA inventory;
                                  CREATE TABLE inventory.skus (
                                      sku_id   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                                      owner_id integer NOT NULL,
                                      code     text NOT NULL);
                                  INSERT INTO inventory.skus (owner_id, code) VALUES
                                      (1, 'HFO-000001'), (1, 'HFO-000002'), (1, 'HFO-JKT-ALPINE-BLK-M'),
                                      (2, 'BWN-000001'), (2, 'BWN-WHEY-VAN-2LB');
                                  """;

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:15-alpine").Build();
    private PostgresSkuCatalogSource _sut = null!;

    [OneTimeSetUp]
    public async Task StartDatabase()
    {
        await _database.StartAsync();

        await using var connection = new NpgsqlConnection(_database.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(Schema, connection);
        await command.ExecuteNonQueryAsync();
    }

    [OneTimeTearDown]
    public async Task StopDatabase()
    {
        await _database.DisposeAsync();
    }

    [SetUp]
    public void SetUp()
    {
        _sut = new PostgresSkuCatalogSource(Options.Create(new SimulatorOptions
        {
            CatalogConnectionString = _database.GetConnectionString()
        }));
    }

    [TearDown]
    public async Task TearDown()
    {
        await _sut.DisposeAsync();
    }

    #region GetSkusAsync

    [Test]
    public async Task GetSkusAsync_OwnerOne_ReturnsOnlyOwnerOnesSkus()
    {
        var actual = await _sut.GetSkusAsync(1, CancellationToken.None);

        Assert.That(actual, Is.EquivalentTo(new[] { "HFO-000001", "HFO-000002", "HFO-JKT-ALPINE-BLK-M" }));
    }

    [Test]
    public async Task GetSkusAsync_CalledTwice_ReturnsSamePopularityOrder()
    {
        var first = await _sut.GetSkusAsync(1, CancellationToken.None);

        var second = await _sut.GetSkusAsync(1, CancellationToken.None);

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public async Task GetSkusAsync_OwnerWithNoSkus_ReturnsEmpty()
    {
        var actual = await _sut.GetSkusAsync(99, CancellationToken.None);

        Assert.That(actual, Is.Empty);
    }

    #endregion
}
