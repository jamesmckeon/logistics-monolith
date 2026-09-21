using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Common.Infrastructure;
using Throughline.Modules.Inventory.Application.Queries;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Infrastructure.Common;
using Throughline.Modules.Inventory.Infrastructure.Orders;

namespace Throughline.Modules.Inventory.Presentation;

public static class InventoryExtensions
{
    public const string InventoryRoute = "/inventory";

    public static IServiceCollection AddInventory(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<OrderAllocationDbContext>(configuration, InfrastructureSettings.SchemaName);

        services.AddScoped<IOrderAllocationRepository, OrderAllocationDbContext>();
        services.AddScoped<GetOrderQuery>();

        return services;
    }

    public static IEndpointRouteBuilder MapInventory(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(InventoryRoute).WithTags("Inventory");

        group.MapGet("/orders",
            async (Guid orderId, GetOrderQuery query, CancellationToken token) =>
                await query.GetOrderByIdAsync(orderId, token));

        return app;
    }
}