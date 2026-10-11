using Microsoft.AspNetCore.Authorization;
using Throughline.Common.Presentation.Auth;

namespace Throughline.Api;

internal static class ThroughlineAuthorizationExtensions
{
    public static IServiceCollection AddThroughlineAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, OwnerRoleHandler>();

        services.AddAuthorizationBuilder()
            .AddPolicy(ThroughlinePolicies.OwnerRole, policy =>
                policy.Requirements.Add(new OwnerRoleRequirement()));

        return services;
    }
}