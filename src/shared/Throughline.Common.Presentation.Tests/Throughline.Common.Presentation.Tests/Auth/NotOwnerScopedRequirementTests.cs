using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Throughline.Common.Presentation.Auth;

namespace Throughline.Common.Presentation.Tests.Auth;

[Category("Unit")]
internal sealed class NotOwnerScopedRequirementTests
{
    private NotOwnerScopedRequirement _sut;

    [SetUp]
    public void SetUp()
    {
        _sut = new NotOwnerScopedRequirement();
    }

    [Test]
    public async Task HandleAsync_UserWithNoRoles_Succeeds()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([], "TestAuthType"));
        var context = new AuthorizationHandlerContext([_sut], user, null);

        await _sut.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.True);
    }
}
