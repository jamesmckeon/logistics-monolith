using Microsoft.EntityFrameworkCore;
using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.Skus;

namespace Throughline.Modules.Ordering.Infrastructure.Skus;

public sealed class SkuAttributesDbContext : DbContext, ISkuAttributesQuery
{
    public SkuAttributesDbContext(DbContextOptions<SkuAttributesDbContext> options) : base(options)
    {
    }

    public Task<IEnumerable<SkuAttributes>> GetAttributesAsync(int merchantId, IEnumerable<SkuCode> skuCodes,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("orders");
    }
}