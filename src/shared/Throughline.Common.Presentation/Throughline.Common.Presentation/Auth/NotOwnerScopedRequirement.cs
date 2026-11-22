using Microsoft.AspNetCore.Authorization;

namespace Throughline.Common.Presentation.Auth;

/// <summary>
///     Marks a policy whose endpoints aren't owner-scoped, such as Platform's, which hold every owner's data. The
///     host leaves the owner role requirement off a policy that includes this requirement. The requirement handles
///     itself and always succeeds, so it adds no check of its own.
/// </summary>
public sealed class NotOwnerScopedRequirement : AuthorizationHandler<NotOwnerScopedRequirement>,
    IAuthorizationRequirement
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, NotOwnerScopedRequirement requirement)
    {
        context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
