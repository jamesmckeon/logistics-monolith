using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.Pricing;

namespace Throughline.Modules.Ordering.Infrastructure.Pricing;

public sealed class PricingDbContext : DbContext, IPickFeeQuery, IZoneChargeQuery, IMerchantRateQuery
{
    private readonly ILogger<PricingDbContext> _logger;

    public PricingDbContext(DbContextOptions<PricingDbContext> options, ILogger<PricingDbContext> logger) :
        base(options)
    {
        _logger = logger;
    }

    public DbSet<MerchantPickFee> PickFees => Set<MerchantPickFee>();
    public DbSet<ZoneSurcharge> ZoneSurcharges => Set<ZoneSurcharge>();
    private DbSet<MerchantRate> MerchantRates => Set<MerchantRate>();

    public async Task<Rate?> GetHandlingAsync(int merchantId, CancellationToken cancellationToken = default)
    {
        var merchantRate = await MerchantRates.SingleOrDefaultAsync(s => s.MerchantId == merchantId, cancellationToken);
        return merchantRate?.Rate;
    }

    public Task<IEnumerable<MerchantPickFee>> GetPickFeesAsync(int merchantId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ZoneSurcharge>> GetChargesAsync(int merchantId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("orders");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PricingDbContext).Assembly);
    }
}