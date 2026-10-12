using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Throughline.Api.Auth;

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
}