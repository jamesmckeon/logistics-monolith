using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Throughline.Api.Auth;
using Throughline.Common.Presentation.Auth;

namespace Throughline.Api.Tests.Auth;

[Category("Unit")]
internal sealed class OwnerRolePolicyProviderTests
{
    private OwnerRolePolicyProvider _sut;

    [SetUp]
    public void SetUp()
    {
        _sut = new OwnerRolePolicyProvider(Options.Create(new AuthorizationOptions()));
    }

    [Test]
    public async Task GetPolicyAsync_PolicyNotRegistered_ReturnsNull()
    {
        var policy = await _sut.GetPolicyAsync("NotRegistered");

        Assert.That(policy, Is.Null);
    }

    [Test]
    public async Task GetPolicyAsync_PolicyNotOwnerScoped_DoesNotRequireOwnerRole()
    {
        const string policyName = "NotOwnerScoped";
        var options = new AuthorizationOptions();
        options.AddPolicy(policyName, p => p
            .RequireRole(ThroughlineRoles.ApplicationSupport)
            .AddRequirements(new NotOwnerScopedRequirement()));
        var sut = new OwnerRolePolicyProvider(Options.Create(options));

        var policy = await sut.GetPolicyAsync(policyName);

        Assert.That(policy?.Requirements, Has.None.InstanceOf<OwnerRoleRequirement>());
    }
    
    [Test]
    public async Task GetPolicyAsync_OwnerScoped_RequiresOwnerRole()
    {
        const string policyName = "OwnerScoped";
        var options = new AuthorizationOptions();
        options.AddPolicy(policyName, p => p.RequireRole(ThroughlineRoles.OutboundPlanning));
        var sut = new OwnerRolePolicyProvider(Options.Create(options));

        var policy = await sut.GetPolicyAsync(policyName);

        Assert.That(policy?.Requirements, Has.One.InstanceOf<OwnerRoleRequirement>());
    }
}