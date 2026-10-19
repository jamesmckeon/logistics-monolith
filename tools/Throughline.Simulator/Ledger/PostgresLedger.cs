using Microsoft.Extensions.Options;
using Npgsql;
using Throughline.Simulator.Configuration;

namespace Throughline.Simulator.Ledger;

/// <summary>
///     The ledger in the simulator's own Postgres database. The schema is created on first use; the
///     simulator's login owns the database, so it needs no migrations tool.
/// </summary>
internal sealed class PostgresLedger : ILedger, IAsyncDisposable
{
    private static readonly TimeSpan ClaimTimeout = TimeSpan.FromMinutes(30);

    private const string Schema = """
                                  CREATE TABLE IF NOT EXISTS sales (
                                      owner_id         integer     NOT NULL,
                                      reference_number text        NOT NULL,
                                      sku              text        NOT NULL,
                                      quantity         integer     NOT NULL,
                                      sold_at          timestamptz NOT NULL,
                                      PRIMARY KEY (owner_id, reference_number, sku));
                                  CREATE INDEX IF NOT EXISTS ix_sales_owner_sold_at ON sales (owner_id, sold_at);

                                  CREATE TABLE IF NOT EXISTS purchase_orders (
                                      purchase_order_id     uuid        PRIMARY KEY,
                                      owner_id              integer     NOT NULL,
                                      purchase_order_number text        NOT NULL,
                                      kind                  text        NOT NULL,
                                      ordered_at            timestamptz NOT NULL);
                                  CREATE INDEX IF NOT EXISTS ix_purchase_orders_owner ON purchase_orders (owner_id, ordered_at);

                                  CREATE TABLE IF NOT EXISTS inbound_shipments (
                                      shipment_id        uuid        PRIMARY KEY,
                                      purchase_order_id  uuid        NOT NULL REFERENCES purchase_orders,
                                      owner_id           integer     NOT NULL,
                                      delivery_reference text        NOT NULL,
                                      bill_of_lading     text        NOT NULL,
                                      carrier_scac       text        NOT NULL,
                                      trailer_number     text        NOT NULL,
                                      shipper_name       text        NOT NULL,
                                      due_at             timestamptz NOT NULL,
                                      claimed_by         text,
                                      claimed_at         timestamptz,
                                      received_at        timestamptz,
                                      rejected_at        timestamptz,
                                      last_status        integer);
                                  CREATE INDEX IF NOT EXISTS ix_inbound_shipments_waiting ON inbound_shipments (due_at)
                                      WHERE received_at IS NULL AND rejected_at IS NULL;

                                  CREATE TABLE IF NOT EXISTS inbound_pallets (
                                      shipment_id uuid    NOT NULL REFERENCES inbound_shipments,
                                      lpn         text    NOT NULL,
                                      sku         text    NOT NULL,
                                      quantity    integer NOT NULL,
                                      location_id text    NOT NULL,
                                      PRIMARY KEY (shipment_id, lpn));
                                  """;

    // created on first use, after the options have been validated
    private readonly Lazy<NpgsqlDataSource> _dataSource;
    private readonly SemaphoreSlim _schemaGate = new(1, 1);
    private bool _schemaReady;

    public PostgresLedger(IOptions<SimulatorOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _dataSource = new Lazy<NpgsqlDataSource>(() => NpgsqlDataSource.Create(options.Value.LedgerConnectionString));
    }

    public async ValueTask DisposeAsync()
    {
        if (_dataSource.IsValueCreated)
            await _dataSource.Value.DisposeAsync();
    }

    public async Task RecordSaleAsync(
        int ownerId, string referenceNumber, IReadOnlyList<SoldLine> lines, DateTimeOffset soldAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var dataSource = await ReadyDataSourceAsync(cancellationToken);
        await using var batch = dataSource.CreateBatch();

        foreach (var line in lines)
        {
            batch.BatchCommands.Add(new NpgsqlBatchCommand(
                "INSERT INTO sales (owner_id, reference_number, sku, quantity, sold_at) " +
                "VALUES ($1, $2, $3, $4, $5) ON CONFLICT DO NOTHING")
            {
                Parameters =
                {
                    new NpgsqlParameter { Value = ownerId },
                    new NpgsqlParameter { Value = referenceNumber },
                    new NpgsqlParameter { Value = line.Sku },
                    new NpgsqlParameter { Value = line.Quantity },
                    new NpgsqlParameter { Value = soldAt.ToUniversalTime() }
                }
            });
        }

        await batch.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, int>> GetUnitsSoldAsync(
        int ownerId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var dataSource = await ReadyDataSourceAsync(cancellationToken);
        await using var command = dataSource.CreateCommand(
            "SELECT sku, sum(quantity)::integer FROM sales " +
            "WHERE owner_id = $1 AND sold_at >= $2 AND sold_at < $3 GROUP BY sku");
        command.Parameters.Add(new NpgsqlParameter { Value = ownerId });
        command.Parameters.Add(new NpgsqlParameter { Value = from.ToUniversalTime() });
        command.Parameters.Add(new NpgsqlParameter { Value = to.ToUniversalTime() });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var unitsSold = new Dictionary<string, int>();

        while (await reader.ReadAsync(cancellationToken))
            unitsSold[reader.GetString(0)] = reader.GetInt32(1);

        return unitsSold;
    }

    public async Task<DateTimeOffset?> GetLastPurchaseOrderAtAsync(int ownerId, CancellationToken cancellationToken)
    {
        var dataSource = await ReadyDataSourceAsync(cancellationToken);
        await using var command = dataSource.CreateCommand(
            "SELECT max(ordered_at) FROM purchase_orders WHERE owner_id = $1");
        command.Parameters.Add(new NpgsqlParameter { Value = ownerId });

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is DateTime orderedAt ? new DateTimeOffset(orderedAt, TimeSpan.Zero) : null;
    }

    public async Task SavePurchaseOrderAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(purchaseOrder);

        var dataSource = await ReadyDataSourceAsync(cancellationToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var batch = new NpgsqlBatch(connection, transaction);

        batch.BatchCommands.Add(new NpgsqlBatchCommand(
            "INSERT INTO purchase_orders (purchase_order_id, owner_id, purchase_order_number, kind, ordered_at) " +
            "VALUES ($1, $2, $3, $4, $5)")
        {
            Parameters =
            {
                new NpgsqlParameter { Value = purchaseOrder.PurchaseOrderId },
                new NpgsqlParameter { Value = purchaseOrder.OwnerId },
                new NpgsqlParameter { Value = purchaseOrder.PurchaseOrderNumber },
                new NpgsqlParameter { Value = purchaseOrder.Kind.ToString() },
                new NpgsqlParameter { Value = purchaseOrder.OrderedAt.ToUniversalTime() }
            }
        });

        foreach (var shipment in purchaseOrder.Shipments)
        {
            batch.BatchCommands.Add(new NpgsqlBatchCommand(
                "INSERT INTO inbound_shipments (shipment_id, purchase_order_id, owner_id, delivery_reference, " +
                "bill_of_lading, carrier_scac, trailer_number, shipper_name, due_at) " +
                "VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)")
            {
                Parameters =
                {
                    new NpgsqlParameter { Value = shipment.ShipmentId },
                    new NpgsqlParameter { Value = purchaseOrder.PurchaseOrderId },
                    new NpgsqlParameter { Value = shipment.OwnerId },
                    new NpgsqlParameter { Value = shipment.DeliveryReference },
                    new NpgsqlParameter { Value = shipment.BillOfLading },
                    new NpgsqlParameter { Value = shipment.CarrierScac },
                    new NpgsqlParameter { Value = shipment.TrailerNumber },
                    new NpgsqlParameter { Value = shipment.ShipperName },
                    new NpgsqlParameter { Value = shipment.DueAt.ToUniversalTime() }
                }
            });

            foreach (var pallet in shipment.Pallets)
            {
                batch.BatchCommands.Add(new NpgsqlBatchCommand(
                    "INSERT INTO inbound_pallets (shipment_id, lpn, sku, quantity, location_id) " +
                    "VALUES ($1, $2, $3, $4, $5)")
                {
                    Parameters =
                    {
                        new NpgsqlParameter { Value = shipment.ShipmentId },
                        new NpgsqlParameter { Value = pallet.Lpn },
                        new NpgsqlParameter { Value = pallet.Sku },
                        new NpgsqlParameter { Value = pallet.Quantity },
                        new NpgsqlParameter { Value = pallet.LocationId }
                    }
                });
            }
        }

        await batch.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<InboundShipment?> ClaimDueShipmentAsync(
        string claimant, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var dataSource = await ReadyDataSourceAsync(cancellationToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // SKIP LOCKED: receivers claiming at the same moment take different trailers
        await using var claim = new NpgsqlCommand(
            """
            UPDATE inbound_shipments SET claimed_by = $1, claimed_at = $2
            WHERE shipment_id = (
                SELECT shipment_id FROM inbound_shipments
                WHERE received_at IS NULL AND rejected_at IS NULL AND due_at <= $2
                  AND (claimed_at IS NULL OR claimed_at < $3)
                ORDER BY due_at
                LIMIT 1
                FOR UPDATE SKIP LOCKED)
            RETURNING shipment_id, owner_id, delivery_reference, bill_of_lading, carrier_scac,
                      trailer_number, shipper_name, due_at
            """, connection, transaction);
        claim.Parameters.Add(new NpgsqlParameter { Value = claimant });
        claim.Parameters.Add(new NpgsqlParameter { Value = now.ToUniversalTime() });
        claim.Parameters.Add(new NpgsqlParameter { Value = (now - ClaimTimeout).ToUniversalTime() });

        Guid shipmentId;
        int ownerId;
        string deliveryReference, billOfLading, carrierScac, trailerNumber, shipperName;
        DateTimeOffset dueAt;

        await using (var reader = await claim.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            shipmentId = reader.GetGuid(0);
            ownerId = reader.GetInt32(1);
            deliveryReference = reader.GetString(2);
            billOfLading = reader.GetString(3);
            carrierScac = reader.GetString(4);
            trailerNumber = reader.GetString(5);
            shipperName = reader.GetString(6);
            dueAt = new DateTimeOffset(reader.GetDateTime(7), TimeSpan.Zero);
        }

        await using var palletsCommand = new NpgsqlCommand(
            "SELECT lpn, sku, quantity, location_id FROM inbound_pallets WHERE shipment_id = $1 ORDER BY lpn",
            connection, transaction);
        palletsCommand.Parameters.Add(new NpgsqlParameter { Value = shipmentId });

        var pallets = new List<InboundPallet>();

        await using (var reader = await palletsCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                pallets.Add(new InboundPallet(reader.GetString(0), reader.GetString(1), reader.GetInt32(2),
                    reader.GetString(3)));
        }

        await transaction.CommitAsync(cancellationToken);

        return new InboundShipment(shipmentId, ownerId, deliveryReference, billOfLading, carrierScac,
            trailerNumber, shipperName, dueAt, pallets);
    }

    public Task MarkReceivedAsync(Guid shipmentId, int statusCode, DateTimeOffset at,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            "UPDATE inbound_shipments SET received_at = $2, last_status = $3 WHERE shipment_id = $1",
            cancellationToken, shipmentId, at.ToUniversalTime(), statusCode);

    public Task MarkRejectedAsync(Guid shipmentId, int statusCode, DateTimeOffset at,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            "UPDATE inbound_shipments SET rejected_at = $2, last_status = $3 WHERE shipment_id = $1",
            cancellationToken, shipmentId, at.ToUniversalTime(), statusCode);

    public Task ReleaseAsync(Guid shipmentId, CancellationToken cancellationToken) =>
        ExecuteAsync(
            "UPDATE inbound_shipments SET claimed_by = NULL, claimed_at = NULL WHERE shipment_id = $1",
            cancellationToken, shipmentId);

    private async Task ExecuteAsync(string sql, CancellationToken cancellationToken, params object[] values)
    {
        var dataSource = await ReadyDataSourceAsync(cancellationToken);
        await using var command = dataSource.CreateCommand(sql);

        foreach (var value in values)
            command.Parameters.Add(new NpgsqlParameter { Value = value });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<NpgsqlDataSource> ReadyDataSourceAsync(CancellationToken cancellationToken)
    {
        if (_schemaReady)
            return _dataSource.Value;

        await _schemaGate.WaitAsync(cancellationToken);

        try
        {
            if (!_schemaReady)
            {
                await using var command = _dataSource.Value.CreateCommand(Schema);
                await command.ExecuteNonQueryAsync(cancellationToken);
                _schemaReady = true;
            }
        }
        finally
        {
            _schemaGate.Release();
        }

        return _dataSource.Value;
    }
}
