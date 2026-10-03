using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Throughline.Modules.Receiving.Contracts.Events;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.DeliveryReceipts;
using Throughline.Modules.Receiving.Domain.Inventory;
using Throughline.Modules.Receiving.Domain.Locations;
using Throughline.Modules.Receiving.Infrastructure.Common;
using Wolverine.EntityFrameworkCore;

namespace Throughline.Modules.Receiving.Infrastructure.Db;

internal sealed class ReceivingDbContext : DbContext, IDeliveryReceiptRepository
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

    public async Task<DeliveryReceipt?> GetReceiptByIdAsync(int ownerId, Guid receiptId, CancellationToken token)
    {
        return await DeliveryReceipts.SingleOrDefaultAsync(s => s.OwnerId == ownerId && s.Id == receiptId, token);
    }

    public async Task<IReadOnlyCollection<ReceivedLpn>> GetReceivedLpnsAsync(int ownerId,
        IEnumerable<UpperCaseString> lpns,
        CancellationToken token) =>
        throw new NotImplementedException();

    public async Task<IReadOnlyCollection<ReceivingLocation>> GetReceivingLocationsAsync(CancellationToken token) =>
        (await Locations.ToListAsync(token)).AsReadOnly();

    public async Task<IReadOnlyCollection<HoldReason>> GetHoldReasonsAsync(int ownerId, CancellationToken token) =>
        (await HoldReasons.ToListAsync(token)).AsReadOnly();

    public Task<ReceiptNumber?> GetLastReceiptNumberAsync(int ownerId, CancellationToken token) =>
        throw new NotImplementedException();

    public async Task AddAsync(
        DeliveryReceipt receipt,
        AllocatablePalletsIntegrationEvent? @event,
        CancellationToken token)
    {
        throw new NotImplementedException("wire up constraint name (see below)");

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(InfrastructureSettings.SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReceivingDbContext).Assembly);
    }
}