using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Api.Tests.Common;
using Throughline.Modules.Ordering.Infrastructure.Orders;

namespace Throughline.Api.Tests.Ordering;

internal sealed class OrderingTestFactory : TestFactoryBase
{
    public override async Task ApplyMigrationsAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}