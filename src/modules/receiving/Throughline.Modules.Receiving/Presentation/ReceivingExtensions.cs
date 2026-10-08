using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Throughline.Common.Infrastructure;
using Throughline.Common.Infrastructure.Logging;
using Throughline.Common.Presentation;
using Throughline.Common.Presentation.Http;
using Throughline.Modules.Receiving.Application.Common;
using Throughline.Modules.Receiving.Application.Configuration;
using Throughline.Modules.Receiving.Application.ReceiveDelivery;
using Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;
using Throughline.Modules.Receiving.Domain.DeliveryReceipts;
using Throughline.Modules.Receiving.Domain.Inventory;
using Throughline.Modules.Receiving.Domain.Shipments;
using Throughline.Modules.Receiving.Domain.Skus;
using Throughline.Modules.Receiving.Infrastructure.Common;
using Throughline.Modules.Receiving.Infrastructure.Db;

namespace Throughline.Modules.Receiving.Presentation;

public static class ReceivingExtensions
{
    public const string ReceivingRoute = "/receiving";

    public static IServiceCollection AddReceiving(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<ReceivingDbContext>(configuration, InfrastructureSettings.SchemaName);

        // Checked at startup so a missing or malformed default location stops the app, rather than failing the
        // first delivery received
        services.AddOptions<AppConfiguration>()
            .Bind(configuration.GetSection(AppConfiguration.SectionName))
            .Validate(
                c => c.HasValidDefaultLocations(),
                $"{AppConfiguration.SectionName}:DefaultLocations must list exactly one location id for each of " +
                string.Join(", ", AppConfiguration.DefaultLocationTypes))
            .ValidateOnStart();

        // Forward each interface the context implements to the same scoped instance.
        // AddScoped<TInterface, InventoryDbContext>() would build a separate context per
        // interface, so repositories and the unit of work would track changes in different contexts.
        services.AddScoped<IDeliveryReceiptRepository>(sp => sp.GetRequiredService<ReceivingDbContext>());
        services.AddScoped<IDeliverySubmissionStore>(sp => sp.GetRequiredService<ReceivingDbContext>());
        services.AddScoped<ICarrierProvider>(sp => sp.GetRequiredService<ReceivingDbContext>());
        services.AddScoped<ISkuProvider>(sp => sp.GetRequiredService<ReceivingDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPalletService, PalletService>();
        services.AddScoped<ReceiveDeliveryHandler>();

        return services;
    }


    public static IEndpointRouteBuilder MapReceiving(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ReceivingRoute).WithTags("Receiving");

        group.MapPost("/receipts", async Task<IResult> (
            ReceiveDeliveryCommand command,
            ReceiveDeliveryHandler handler,
            RequestContext requestContext,
            ILoggerFactory loggerFactory,
            CancellationToken token) =>
        {
            using var _ = LoggerScopeFactory.OwnerScope(
                loggerFactory.CreateLogger("Receiving"),
                requestContext.OwnerId);

            var result = await handler.ReceiveDeliveryAsync(requestContext.OwnerId, command, token);

            if (!result.Succeeded)
            {
                return result.ToFailureResponse();
            }

            return TypedResults.Ok(result.Value);
        });

        return app;
    }
}