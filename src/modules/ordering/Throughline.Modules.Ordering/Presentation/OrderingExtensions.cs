using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Common.Presentation;
using Throughline.Modules.Ordering.Application.CreateOrder;
using Throughline.Modules.Ordering.Infrastructure.Orders;

namespace Throughline.Modules.Ordering.Presentation;

public static class OrderingExtensions
{
    public const string OrdersRoute = "/orders";

    public static IServiceCollection AddOrdering(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<OrdersDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("Throughline")));

        services.AddScoped<OrdersRepository>();
        services.AddScoped<CreateOrderHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapOrdering(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(OrdersRoute).WithTags("Ordering");

        group.MapPost("/", async (CancellationToken token,
            CreateOrderCommand command, CreateOrderHandler handler) =>
        {
            var result = await handler.CreateOrderAsync(command, token);
            return result.Ok();
        });

        return app;
    }
}