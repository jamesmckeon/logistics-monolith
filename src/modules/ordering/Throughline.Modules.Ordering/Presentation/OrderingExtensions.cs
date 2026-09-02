using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Common.Presentation;
using Throughline.Modules.Ordering.Application.CreateOrder;
using Throughline.Modules.Ordering.Infrastructure.Orders;

namespace Throughline.Modules.Ordering.Presentation;

public static class OrderingExtensions
{
    public static IServiceCollection AddOrdering(this IServiceCollection services, IConfiguration config)
    {
        services.AddTransient<CreateOrderHandler>();
        services.AddTransient<OrdersDbContext>();
        return services;
    }

    public static IEndpointRouteBuilder MapOrdering(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").WithTags("Ordering");

        group.MapPost("/", async (CancellationToken token,
            CreateOrderCommand command, CreateOrderHandler handler) =>
        {
            var result = await handler.CreateOrderAsync(command, token);
            return result.ToTypedResult();
        });

        return app;
    }
}