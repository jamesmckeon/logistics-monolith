using Microsoft.AspNetCore.Authorization;
using Throughline.Common.Presentation.Http;

namespace Throughline.Common.Presentation.Auth;

public sealed class OwnerRoleHandler : AuthorizationHandler<OwnerRoleRequirement>
{
    private readonly IRequestContext _requestContext;

    public OwnerRoleHandler(IRequestContext requestContext)
    {
        _requestContext = requestContext;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, OwnerRoleRequirement requirement)
    {
        var headerValue = _requestContext.GetHeaderValue(RequestContext.OwnerIdHeaderKey);

        if (int.TryParse(headerValue, out var ownerId))
        {
            if (context.User.IsInRole(
                    ThroughlineRoles.OwnerRole(
                        Convert.ToInt32(ownerId))))
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}