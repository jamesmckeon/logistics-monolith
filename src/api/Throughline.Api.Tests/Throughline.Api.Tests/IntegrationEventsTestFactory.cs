using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using InventoryOrdersDbContext = Throughline.Modules.Inventory.Infrastructure.Orders.OrdersDbContext;
using OrderingOrdersDbContext = Throughline.Modules.Ordering.Infrastructure.Orders.OrdersDbContext;

namespace Throughline.Api.Tests;

internal sealed class IntegrationEventsTestFactory : TestFactoryBase
{
    public override async Task ApplyMigrationsAsync()
    {
        // This test spans both modules (POST to Ordering -> OrderConfirmed -> Inventory),
        // so both schemas must exist.
        using var scope = Services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<OrderingOrdersDbContext>()
            .Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<InventoryOrdersDbContext>()
            .Database.MigrateAsync();
    }
}
