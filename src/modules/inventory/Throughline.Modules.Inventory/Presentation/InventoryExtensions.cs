using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Common.Infrastructure;
using Throughline.Modules.Inventory.Domain.Orders;
using Throughline.Modules.Inventory.Infrastructure.Orders;

namespace Throughline.Modules.Inventory.Presentation;

public static class InventoryExtensions
{
    public static IServiceCollection AddInventory(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<OrdersDbContext>(configuration);

        services.AddScoped<IOrdersRepository, OrdersDbContext>();

        return services;
    }
}
