using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Api.Auth;
using Throughline.Common.Presentation.Auth;

namespace Throughline.Api.Tests.Auth;

[Category("Unit")]
internal sealed class ThroughlineAuthorizationExtensionsTests
{
    private const string ModulePolicy = "ModulePolicy";
    private const string ModuleRole = "ModuleRole";

    #region AddThroughlineAuthorization

    [Test]
    public async Task AddThroughlineAuthorization_NoAuthorizationDeclared_FallbackRequiresTokenAndOwnerRole()
    {
        var services = new ServiceCollection();

        services.AddThroughlineAuthorization();
        await using var provider = services.BuildServiceProvider();
        var policy = await provider.GetRequiredService<IAuthorizationPolicyProvider>().GetFallbackPolicyAsync();

        Assert.Multiple(() =>
        {
            Assert.That(policy?.Requirements, Has.Exactly(1).InstanceOf<DenyAnonymousAuthorizationRequirement>());
            Assert.That(policy?.Requirements, Has.Exactly(1).InstanceOf<OwnerRoleRequirement>());
        });
    }

    [Test]
    public async Task AddThroughlineAuthorization_NoPolicyNamed_DefaultRequiresTokenAndOwnerRole()
    {
        var services = new ServiceCollection();

        services.AddThroughlineAuthorization();
        await using var provider = services.BuildServiceProvider();
        var policy = await provider.GetRequiredService<IAuthorizationPolicyProvider>().GetDefaultPolicyAsync();

        Assert.Multiple(() =>
        {
            Assert.That(policy.Requirements, Has.Exactly(1).InstanceOf<DenyAnonymousAuthorizationRequirement>());
            Assert.That(policy.Requirements, Has.Exactly(1).InstanceOf<OwnerRoleRequirement>());
        });
    }

    [Test]
    public async Task AddThroughlineAuthorization_ModulePolicyNamed_PolicyAlsoRequiresOwnerRole()
    {
        var services = new ServiceCollection();
        // a module registers its policies after the host has added authorization
        services.AddThroughlineAuthorization();
        services.AddAuthorizationBuilder()
            .AddPolicy(ModulePolicy, p => p.RequireRole(ModuleRole));

        await using var provider = services.BuildServiceProvider();
        var policy = await provider.GetRequiredService<IAuthorizationPolicyProvider>().GetPolicyAsync(ModulePolicy);

        Assert.Multiple(() =>
        {
            Assert.That(
                policy?.Requirements.OfType<RolesAuthorizationRequirement>().Single().AllowedRoles,
                Is.EqualTo(new[] { ModuleRole }));
            Assert.That(policy?.Requirements, Has.Exactly(1).InstanceOf<OwnerRoleRequirement>());
        });
    }

    #endregion
}