using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Api.Tests.Common;
using Throughline.Modules.Inventory.Infrastructure.Db;

namespace Throughline.Api.Tests.Inventory;

internal sealed class InventoryTestFactory : TestFactoryBase
{
    public override async Task ApplyMigrationsAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}