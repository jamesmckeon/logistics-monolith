using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Modules.Inventory.Infrastructure.Orders;
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
        await scope.ServiceProvider.GetRequiredService<OrderAllocationDbContext>()
            .Database.MigrateAsync();
    }
}