using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Common.Infrastructure;
using Throughline.Common.Presentation;
using Throughline.Common.Presentation.Auth;
using Throughline.Modules.Platform.Application.Common;
using Throughline.Modules.Platform.Application.Queries;
using Throughline.Modules.Platform.Auth;
using Throughline.Modules.Platform.Infrastructure.Common;
using Throughline.Modules.Platform.Infrastructure.Db;

namespace Throughline.Modules.Platform.Presentation;

public static class PlatformExtensions
{
    public const string PlatformRoute = "/platform";

    public static IServiceCollection AddPlatform(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<PlatformDbContext>(configuration, InfrastructureSettings.SchemaName);
        services.AddScoped<IDeadLetterStore>(sp => sp.GetRequiredService<PlatformDbContext>());
        services.AddScoped<GetDeadLettersQuery>();

        // Dead letters hold every owner's messages, so Platform's policies aren't owner-scoped
        services.AddAuthorizationBuilder()
            .AddPolicy(PlatformPolicies.ReadDeadLetters, p => p
                .RequireRole(ThroughlineRoles.ApplicationSupport)
                .AddRequirements(new NotOwnerScopedRequirement()));

        return services;
    }

    public static IEndpointRouteBuilder MapPlatform(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(PlatformRoute).WithTags("Platform");

        group.MapGet("/dead-letters", async Task<IResult> (
            int? ownerId,
            string? module,
            GetDeadLettersQuery query,
            CancellationToken token) =>
        {
            var result = await query.GetDeadLettersAsync(ownerId, module, token);

            return result.Succeeded ? TypedResults.Ok(result.Value) : result.ToFailureResponse();
        }).RequireAuthorization(PlatformPolicies.ReadDeadLetters);

        group.MapGet("/dead-letters/{deadLetterId:guid}", async Task<IResult> (
            Guid deadLetterId,
            GetDeadLettersQuery query,
            CancellationToken token) =>
        {
            var result = await query.GetDeadLetterAsync(deadLetterId, token);

            return result.Succeeded ? TypedResults.Ok(result.Value) : result.ToFailureResponse();
        }).RequireAuthorization(PlatformPolicies.ReadDeadLetters);

        return app;
    }
}
