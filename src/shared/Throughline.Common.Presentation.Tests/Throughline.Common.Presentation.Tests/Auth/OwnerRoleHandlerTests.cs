using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Throughline.Common.Presentation.Auth;
using Throughline.Common.Presentation.Http;

namespace Throughline.Common.Presentation.Tests.Auth;

[Category("Unit")]
internal sealed class OwnerRoleHandlerTests
{
    private Mock<IRequestContext> Context { get; set; }
    private OwnerRoleHandler Sut { get; set; }

    [SetUp]
    public void Setup()
    {
        Context = new();
        Sut = new(Context.Object);
    }

    [Test]
    public async Task HandleRequirementAsync_NoUserHeader_DoesntSucceed()
    {
        var identity = new ClaimsIdentity([], "TestAuthType");
        var user = new ClaimsPrincipal(identity);
        var context = new AuthorizationHandlerContext([new OwnerRoleRequirement()], user, null);

        await Sut.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.False);
    }

    [Test]
    public async Task HandleRequirementAsync_DifferentOwner_DoesntSucceed()
    {
        var ownerClaim = new Claim(ClaimTypes.Role, ThroughlineRoles.OwnerRole(1));
        var identity = new ClaimsIdentity([ownerClaim], "TestAuthType");
        var user = new ClaimsPrincipal(identity);
        var context = new AuthorizationHandlerContext([new OwnerRoleRequirement()], user, null);

        Context.Setup(s => s.GetHeaderValue(RequestContext.OwnerIdHeaderKey))
            .Returns("2");

        await Sut.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.False);
    }

    [Test]
    public async Task HandleRequirementAsync_SameOwner_Succeeds()
    {
        var ownerClaim = new Claim(ClaimTypes.Role, ThroughlineRoles.OwnerRole(1));
        var identity = new ClaimsIdentity([ownerClaim], "TestAuthType");
        var user = new ClaimsPrincipal(identity);
        var context = new AuthorizationHandlerContext([new OwnerRoleRequirement()], user, null);

        Context.Setup(s => s.GetHeaderValue(RequestContext.OwnerIdHeaderKey))
            .Returns("1");

        await Sut.HandleAsync(context);

        Assert.That(context.HasSucceeded, Is.True);
    }
}