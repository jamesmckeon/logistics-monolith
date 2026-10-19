using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Throughline.Simulator.Configuration;
using Throughline.Simulator.Ledger;

namespace Throughline.Simulator.Tests.Ledger;

// What only a real Postgres can show: the schema creates itself, claims skip trailers that aren't
// due or are already taken, and a resent order isn't counted twice
[Category("Integration")]
public sealed class PostgresLedgerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 15, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:15-alpine").Build();
    private PostgresLedger _sut = null!;

    [OneTimeSetUp]
    public async Task StartDatabase()
    {
        await _database.StartAsync();
    }

    [OneTimeTearDown]
    public async Task StopDatabase()
    {
        await _database.DisposeAsync();
    }

    [SetUp]
    public void SetUp()
    {
        _sut = new PostgresLedger(Options.Create(new SimulatorOptions
        {
            LedgerConnectionString = _database.GetConnectionString()
        }));
    }

    [TearDown]
    public async Task TearDown()
    {
        await _sut.DisposeAsync();
    }

    [Test]
    public async Task ClaimDueShipmentAsync_OneDueOneNot_ClaimsTheDueTrailerOnce()
    {
        var due = Shipment(ownerId: 41, dueAt: Now.AddHours(-1));
        var notYetDue = Shipment(ownerId: 41, dueAt: Now.AddDays(1));
        await _sut.SavePurchaseOrderAsync(
            new PurchaseOrder(Guid.NewGuid(), 41, "HFO-PO-1", PurchaseOrderKind.Weekly, Now.AddDays(-7), [due, notYetDue]),
            CancellationToken.None);

        var first = await _sut.ClaimDueShipmentAsync("receiver1", Now, CancellationToken.None);
        var second = await _sut.ClaimDueShipmentAsync("receiver2", Now, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first?.ShipmentId, Is.EqualTo(due.ShipmentId));
            Assert.That(first?.Pallets, Is.EqualTo(due.Pallets));
            Assert.That(second, Is.Null);
        });
    }

    [Test]
    public async Task RecordSaleAsync_SameOrderTwice_CountedOnce()
    {
        SoldLine[] lines = [new("HFO-000001", 2), new("HFO-000002", 1)];
        await _sut.RecordSaleAsync(42, "HFO-20261019-A1B2-000001", lines, Now, CancellationToken.None);
        await _sut.RecordSaleAsync(42, "HFO-20261019-A1B2-000001", lines, Now, CancellationToken.None);

        var actual = await _sut.GetUnitsSoldAsync(42, Now.AddDays(-1), Now.AddDays(1), CancellationToken.None);

        Assert.That(actual, Is.EquivalentTo(new Dictionary<string, int> { ["HFO-000001"] = 2, ["HFO-000002"] = 1 }));
    }

    private static InboundShipment Shipment(int ownerId, DateTimeOffset dueAt)
    {
        var id = Guid.NewGuid();
        var lpn = ("LPN" + id.ToString("N"))[..15].ToUpperInvariant();

        return new InboundShipment(id, ownerId, $"HFO-PO-1-{lpn}", "BOL0000000001", "ODFL", "TRL00001",
            "Ridgeline Apparel Manufacturing", dueAt, [new InboundPallet(lpn, "HFO-000001", 48, "BULK-01")]);
    }
}
