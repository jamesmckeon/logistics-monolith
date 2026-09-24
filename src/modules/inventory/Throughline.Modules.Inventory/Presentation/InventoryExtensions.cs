using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Common.Infrastructure;
using Throughline.Modules.Inventory.Application.Queries;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Domain.Owners;
using Throughline.Modules.Inventory.Infrastructure.Common;
using Throughline.Modules.Inventory.Infrastructure.Db;
using Throughline.Modules.Inventory.Infrastructure.Events;

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
        services.AddScoped<IOrderConfirmedEventService, OrderConfirmedEventService>();
        services.AddScoped<IOrderlineAllocationService, OrderLineAllocationService>();
        services.AddScoped<AllocationSpecContext>();
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