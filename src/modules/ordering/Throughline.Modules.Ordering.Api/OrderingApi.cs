using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Modules.Ordering.Application.CreateOrder;
using Throughline.Modules.Ordering.Application.Orders.Models;

namespace Throughline.Modules.Ordering.Api;

public static class OrderingExtensions
{
    public static IServiceCollection AddOrdering(this IServiceCollection services, IConfiguration config)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapOrdering(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").WithTags("Ordering");
        group.MapPost("/", async (CancellationToken token,
            CreateOrderRequest request, ICreateOrderHandler handler) =>
        {
            var commandResult = CreateOrderCommand.Create(
                request.MerchantId,
                request.PurchaseOrderNumber,
                request.StreetAddressOne,
                request.StreetAddressTwo,
                request.City,
                request.State,
                request.PostalCode,
                request.Items,
                request.ReferenceNumber);

            if (!commandResult.Succeeded)
                return Results.BadRequest();

            var result = await handler.CreateOrderAsync(commandResult.Value, token);

            if (result.Succeeded)
                return result.Value;
            // what do i return here
            throw new NotImplementedException();
        });

        return app;
    }
}