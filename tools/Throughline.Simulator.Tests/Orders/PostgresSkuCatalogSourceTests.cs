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
    // it can't run the item master's changesets; a change to master_data.skus surfaces when it runs.
    private const string Schema = """
                                  CREATE SCHEMA master_data;
                                  CREATE TABLE master_data.skus (
                                      sku_id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                                      owner_id        integer NOT NULL,
                                      code            character varying(50) NOT NULL,
                                      carton_quantity integer,
                                      pallet_quantity integer);
                                  INSERT INTO master_data.skus (owner_id, code, carton_quantity, pallet_quantity) VALUES
                                      (1, 'HFO-000001', 24, 720), (1, 'HFO-000002', 12, 360),
                                      (1, 'HFO-JKT-ALPINE-BLK-M', 6, NULL),
                                      (2, 'BWN-000001', NULL, NULL), (2, 'BWN-WHEY-VAN-2LB', NULL, NULL);
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
    public async Task GetSkusAsync_OwnerOne_ReturnsOwnerOnesSkusWithQuantities()
    {
        var actual = await _sut.GetSkusAsync(1, CancellationToken.None);

        Assert.That(actual, Is.EquivalentTo(new[]
        {
            new ItemMasterSku("HFO-000001", 24, 720),
            new ItemMasterSku("HFO-000002", 12, 360),
            new ItemMasterSku("HFO-JKT-ALPINE-BLK-M", 6, null)
        }));
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
