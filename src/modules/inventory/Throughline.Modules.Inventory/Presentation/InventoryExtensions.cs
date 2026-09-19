using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Common.Infrastructure;
using Throughline.Modules.Inventory.Application.Queries;
using Throughline.Modules.Inventory.Domain.Orders;
using Throughline.Modules.Inventory.Infrastructure.Orders;

namespace Throughline.Modules.Inventory.Presentation;

public static class InventoryExtensions
{
    public const string InventoryRoute = "/inventory";

    public static IServiceCollection AddInventory(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<OrdersDbContext>(configuration);

        services.AddScoped<IOrdersRepository, OrdersDbContext>();

        return services;
    }

    public static IEndpointRouteBuilder MapInventory(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(InventoryRoute).WithTags("Inventory");

        group.MapGet("/orders",
            async (int ownerId, Guid orderId, GetOrderQuery query, CancellationToken token) =>
                await query.GetOrderByOwnerOrderIdAsync(ownerId, orderId, token));

        return app;
    }
}