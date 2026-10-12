using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Throughline.Common.Presentation.Auth;

namespace Throughline.Api.Auth;

/// <summary>
///     Adds the owner role requirement to every named policy, so an endpoint that requires a
///     policy is always checked for the owner named in the request as well
/// </summary>
internal sealed class OwnerRolePolicyProvider : DefaultAuthorizationPolicyProvider
{
    public OwnerRolePolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    // Policies are fixed once the host is built, so the combined policy of an endpoint can be cached
    public override bool AllowsCachingPolicies => true;

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = await base.GetPolicyAsync(policyName);
        if (policy is null)
        {
            return null;
        }

        return new AuthorizationPolicyBuilder(policy)
            .AddRequirements(new OwnerRoleRequirement())
            .Build();
    }
}
