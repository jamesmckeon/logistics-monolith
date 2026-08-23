using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Common.Presentation;
using Throughline.Modules.Ordering.Application.CreateOrder;
using Throughline.Modules.Ordering.Application.Orders.Models;
using Throughline.Modules.Ordering.Domain.OrderEstimates;
using Throughline.Modules.Ordering.Domain.Orders;
using Throughline.Modules.Ordering.Domain.Pricing;
using Throughline.Modules.Ordering.Domain.Skus;
using Throughline.Modules.Ordering.Infrastructure.Orders;
using Throughline.Modules.Ordering.Infrastructure.Pricing;
using Throughline.Modules.Ordering.Infrastructure.Skus;

namespace Throughline.Modules.Ordering.Api;

public static class OrderingExtensions
{
    public static IServiceCollection AddOrdering(this IServiceCollection services, IConfiguration config)
    {
        services.AddTransient<ICreateOrderHandler, CreateOrderHandler>();
        services.AddTransient<IOrderEstimateRequestBuilder, OrderEstimateRequestBuilder>();
        services.AddTransient<IOrderEstimateService, OrderEstimateService>();
        services.AddTransient<IOrdersRepository, OrdersRepository>();
        services.AddTransient<IMerchantRateQuery, PricingDbContext>();
        services.AddTransient<IPickFeeQuery, PricingDbContext>();
        services.AddTransient<IZoneChargeQuery, PricingDbContext>();
        services.AddTransient<ISkuAttributesQuery, SkuAttributesDbContext>();

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
                return commandResult.ToTypedResult();

            var result = await handler.CreateOrderAsync(commandResult.Value, token);

            return result.ToTypedResult();
        });

        return app;
    }
}