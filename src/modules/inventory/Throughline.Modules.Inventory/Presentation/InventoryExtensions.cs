using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Common.Infrastructure;
using Throughline.Common.Presentation.Http;
using Throughline.Modules.Inventory.Application.AllocateOrders;
using Throughline.Modules.Inventory.Application.Queries;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Domain.Owners;
using Throughline.Modules.Inventory.Infrastructure.Common;
using Throughline.Modules.Inventory.Infrastructure.Db;

namespace Throughline.Modules.Inventory.Presentation;

public static class InventoryExtensions
{
    public const string InventoryRoute = "/inventory";

    public static IServiceCollection AddInventory(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<InventoryDbContext>(configuration, InfrastructureSettings.SchemaName);

        // Forward each interface the context implements to the same scoped instance.
        // AddScoped<TInterface, InventoryDbContext>() would build a separate context per
        // interface, so repositories and the unit of work would track changes in different contexts.
        services.AddScoped<IOrderAllocationRepository>(sp => sp.GetRequiredService<InventoryDbContext>());
        services.AddScoped<IInventoryRepository>(sp => sp.GetRequiredService<InventoryDbContext>());
        services.AddScoped<IOwnerProvider>(sp => sp.GetRequiredService<InventoryDbContext>());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAllocationService, AllocationService>();
        services.AddScoped<IOrderAllocationService, OrderAllocationService>();
        services.AddScoped<AllocateOrdersHandler>();
        services.AddScoped<GetOrderQuery>();

        return services;
    }

    public static IEndpointRouteBuilder MapInventory(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(InventoryRoute).WithTags("Inventory");

        group.MapGet("/orders/{orderId}",
            async (Guid orderId, RequestContext requestContext, GetOrderQuery query, CancellationToken token) =>
                await query.GetOrderByIdAsync(requestContext.OwnerId, orderId, token) is { } order
                    ? Results.Ok(order)
                    : Results.NotFound());


        group.MapPost("/orders/allocate", async Task<IResult> (
            AllocateOrdersCommand command,
            AllocateOrdersHandler hander, CancellationToken token) =>
        {
            var result = await hander.AllocateOrdersAsync(command, token);

            if (result.IsBadRequest)
            {
                return TypedResults.BadRequest(result.Errors);
            }

            return TypedResults.Ok(result);
        });

        return app;
    }
}