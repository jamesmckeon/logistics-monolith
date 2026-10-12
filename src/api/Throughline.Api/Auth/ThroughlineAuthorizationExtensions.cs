using Microsoft.AspNetCore.Authorization;
using Throughline.Common.Presentation.Auth;

namespace Throughline.Api.Auth;

internal static class ThroughlineAuthorizationExtensions
{
    public static IServiceCollection AddThroughlineAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, OwnerRoleHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, OwnerRolePolicyProvider>();

        var authenticatedOwnerRole = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new OwnerRoleRequirement())
            .Build();

        services.AddAuthorizationBuilder()
            // Applies to an endpoint that requires authorization without naming a policy
            .SetDefaultPolicy(authenticatedOwnerRole)
            // Applies to every endpoint that declares no authorization of its own, so an
            // endpoint is never anonymous by omission
            .SetFallbackPolicy(authenticatedOwnerRole);

        return services;
    }
}