using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Throughline.Common.Models;
using Throughline.Modules.Receiving.Application.ReceiveDelivery;
using Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;
using Throughline.Modules.Receiving.Contracts.Events;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.DeliveryReceipts;
using Throughline.Modules.Receiving.Domain.Inventory;
using Throughline.Modules.Receiving.Domain.Locations;
using Throughline.Modules.Receiving.Domain.Shipments;
using Throughline.Modules.Receiving.Domain.Skus;
using Throughline.Modules.Receiving.Infrastructure.Common;
using Throughline.Modules.Receiving.Infrastructure.Db.Converters;
using Throughline.Modules.Receiving.Infrastructure.Db.Models;
using Wolverine.EntityFrameworkCore;

namespace Throughline.Modules.Receiving.Infrastructure.Db;

internal sealed class ReceivingDbContext :
    DbContext, IDeliveryReceiptRepository, IDeliverySubmissionStore, ICarrierProvider, ISkuProvider
{
    private readonly ILogger<ReceivingDbContext> _logger;
    private readonly IDbContextOutbox _outbox;

    // Public ctor is required by AddDbContext (EF resolves the context through DI); the type
    // itself stays internal, so the module boundary is unaffected.
    public ReceivingDbContext(
        DbContextOptions<ReceivingDbContext> options,
        IDbContextOutbox outbox,
        ILogger<ReceivingDbContext> logger) : base(options)
    {
        _outbox = outbox;
        _logger = logger;
    }

    public DbSet<DeliveryReceipt> DeliveryReceipts => Set<DeliveryReceipt>();
    public DbSet<ReceivingLocation> Locations => Set<ReceivingLocation>();
    public DbSet<HoldReason> HoldReasons => Set<HoldReason>();
    public DbSet<DeliverySubmission> DeliverySubmissions => Set<DeliverySubmission>();

    public DbSet<CarrierRecord> Carriers => Set<CarrierRecord>();

    public DbSet<SkuRecord> Skus => Set<SkuRecord>();

    public DbSet<ReceiptNumberCounterRecord> ReceiptNumberCounters => Set<ReceiptNumberCounterRecord>();

    async Task<CarrierScac?> ICarrierProvider.GetCarrierScacByScacCodeAync(ScacCode scacCode, CancellationToken token)
    {
        var carrier = await Carriers.SingleOrDefaultAsync(c => c.ScacCode == scacCode.Value, token);

        return carrier is null
            ? null
            : new CarrierScac(new ScacCode(carrier.ScacCode), carrier.CarrierId, carrier.CarrierName);
    }

    public async Task<IReadOnlyCollection<ReceivingLocation>> GetReceivingLocationsAsync(CancellationToken token) =>
        (await Locations.ToListAsync(token)).AsReadOnly();

    public async Task<IReadOnlyCollection<HoldReason>> GetHoldReasonsAsync(int ownerId, CancellationToken token) =>
        (await HoldReasons.ToListAsync(token)).AsReadOnly();

    // One atomic statement: creates the owner's counter at 1 on first use, otherwise increments it, and returns the
    // number issued. Concurrent calls for an owner queue on that owner's row only. It must commit on its own (see
    // IDeliveryReceiptRepository), so it refuses to run inside a transaction, where it would hold the owner's row
    // locked until that transaction ended.
    public async Task<ReceiptNumber> NextReceiptNumberAsync(int ownerId, CancellationToken token)
    {
        if (Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException("A receipt number must be issued outside a transaction");
        }

        // Not composed with LINQ: EF would wrap the SQL in a subquery, and PostgreSQL doesn't allow an
        // INSERT ... RETURNING there
        var issued = await Database.SqlQuery<long>(
                $"""
                 INSERT INTO receiving.receipt_number_counters AS c (owner_id, last_number)
                 VALUES ({ownerId}, 1)
                 ON CONFLICT (owner_id) DO UPDATE SET last_number = c.last_number + 1
                 RETURNING c.last_number AS "Value"
                 """)
            .ToListAsync(token);

        return ReceiptNumber.FromSequence(issued.Single());
    }

    public async Task AddAsync(
        DeliveryReceipt receipt,
        AllocatablePalletsIntegrationEvent? @event,
        CancellationToken token)
    {
        DeliveryReceipts.Add(receipt);

        _outbox.Enroll(this);
        await _outbox.PublishAsync(@event);

        try
        {
            await _outbox.SaveChangesAndFlushMessagesAsync(token);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "Put constraint name here"
            })
        {
            _logger.LogInformation("An existing receipt was found for receipt id {@ReceiptId}", receipt.Id);
        }
    }

    public async Task<IReadOnlyCollection<UpperCaseString>> GetReceivedLpnsAsync(int ownerId,
        IEnumerable<UpperCaseString> lpns,
        CancellationToken token)
    {
        return await DeliveryReceipts.Where(r => r.OwnerId == ownerId)
            .SelectMany(s => s.Pallets)
            .Where(p => lpns.Any(a => a == p.LicensePlateNumber))
            .Select(p => p.LicensePlateNumber)
            .ToListAsync(token);
    }

    // Implemented explicitly so this Add doesn't sit among DbContext's own Add overloads
    Task<DeliverySubmission?> IDeliverySubmissionStore.GetSubmissionAsync(
        int ownerId, Guid deliveryId, CancellationToken token) =>
        DeliverySubmissions.SingleOrDefaultAsync(s => s.OwnerId == ownerId && s.DeliveryId == deliveryId, token);

    void IDeliverySubmissionStore.Add(DeliverySubmission submission) => DeliverySubmissions.Add(submission);

    async Task<IReadOnlyCollection<OwnerSku>> ISkuProvider.GetSkusByOwnerSkuCodeAsync(
        int ownerId, IEnumerable<UpperCaseString> skuCodes, CancellationToken token)
    {
        var codes = skuCodes.Select(c => c.Value).Distinct().ToArray();

        var skus = await Skus
            .Where(s => s.OwnerId == ownerId && codes.Contains(s.SkuCode))
            .ToListAsync(token);

        return skus.Select(s => new OwnerSku(
                s.OwnerId, new UpperCaseString(s.SkuCode), s.SkuId, s.IsLotTracked, s.IsExpirationTracked))
            .ToList()
            .AsReadOnly();
    }

    public async Task<DeliveryReceipt?> GetReceiptByIdAsync(int ownerId, UniqueId receiptId, CancellationToken token)
    {
        return await DeliveryReceipts.SingleOrDefaultAsync(s => s.OwnerId == ownerId && s.Id == receiptId, token);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(InfrastructureSettings.SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReceivingDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<AppDateTime>().HaveConversion<AppDateTimeValueConverter>();
        configurationBuilder.Properties<NonFutureDateTime>().HaveConversion<NonFutureDateTimeValueConverter>();
        configurationBuilder.Properties<UniqueId>().HaveConversion<UniqueIdValueConverter>();
        configurationBuilder.Properties<UpperCaseString>().HaveConversion<UpperCaseStringValueConverter>();
        configurationBuilder.Properties<TrimmedString>().HaveConversion<TrimmedStringValueConverter>();
        configurationBuilder.Properties<ScacCode>().HaveConversion<ScacCodeValueConverter>();
        configurationBuilder.Properties<ReceiptNumber>().HaveConversion<ReceiptNumberValueConverter>();
    }
}