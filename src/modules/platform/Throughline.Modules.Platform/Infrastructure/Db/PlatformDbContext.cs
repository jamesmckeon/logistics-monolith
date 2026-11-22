using Microsoft.EntityFrameworkCore;
using Throughline.Modules.Platform.Application.Common;
using Throughline.Modules.Platform.Application.Models;
using Throughline.Modules.Platform.Infrastructure.Common;

namespace Throughline.Modules.Platform.Infrastructure.Db;

internal sealed class PlatformDbContext : DbContext, IDeadLetterStore
{
    // Public ctor is required by AddDbContext (EF resolves the context through DI); the type
    // itself stays internal, so the module boundary is unaffected.
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options) : base(options)
    {
    }

    public DbSet<DeadLetterRecord> DeadLetters => Set<DeadLetterRecord>();

    public async Task<IReadOnlyCollection<DeadLetterRecord>> GetDeadLettersAsync(CancellationToken token)
    {
        return await DeadLetters.AsNoTracking().ToListAsync(token);
    }

    public Task<DeadLetterRecord?> GetDeadLetterAsync(Guid deadLetterId, CancellationToken token)
    {
        return DeadLetters.AsNoTracking().SingleOrDefaultAsync(d => d.Id == deadLetterId, token);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(InfrastructureSettings.SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformDbContext).Assembly);
    }
}
