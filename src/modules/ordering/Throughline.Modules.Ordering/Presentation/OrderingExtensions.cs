using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Common.Presentation;
using Throughline.Common.Presentation.Http;
using Throughline.Modules.Ordering.Application.CreateOrder;
using Throughline.Modules.Ordering.Application.Models;
using Throughline.Modules.Ordering.Application.Queries;
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
        services.AddScoped<GetOrderByIdQuery>();

        services.AddHttpContextAccessor();
        services.AddScoped<RequestContext>();

        return services;
    }

    public static IEndpointRouteBuilder MapOrdering(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(OrdersRoute).WithTags("Ordering");

        group.MapPost("/", async (CancellationToken token,
            CreateOrderCommand command, CreateOrderHandler handler) =>
        {
            var result = await handler.CreateOrderAsync(command, token);
            var uri = result.Succeeded ? $"{OrdersRoute}/{result.Value.OrderId}" : null;

            return result.Created(uri);
        });

        group.MapGet("/{orderId}", async Task<Results<Ok<OrderModel>, NotFound>> (CancellationToken token,
            Guid orderId, RequestContext requestContext, GetOrderByIdQuery query) =>
        {
            var model = await query.GetOrderByIdAsync(orderId, requestContext.OwnerId, token);

            if (model is null)
                return TypedResults.NotFound();

            return TypedResults.Ok(model);
        });


        return app;
    }
}