using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Modules.Inventory.Infrastructure.Db;
using OrderingOrdersDbContext = Throughline.Modules.Ordering.Infrastructure.Orders.OrdersDbContext;

namespace Throughline.Api.Tests.Common;

internal sealed class IntegrationEventsTestFactory : TestFactory
{
    public async Task ApplyMigrationsAsync()
    {
        // This test spans both modules (POST to Ordering -> OrderConfirmed -> Inventory),
        // so both schemas must exist.
        using var scope = Services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<OrderingOrdersDbContext>()
            .Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<InventoryDbContext>()
            .Database.MigrateAsync();
    }
}