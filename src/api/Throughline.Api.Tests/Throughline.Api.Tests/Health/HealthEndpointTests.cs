using System.Net;
using Throughline.Api.Health;
using Throughline.Api.Tests.Common;

namespace Throughline.Api.Tests.Health;

/// <summary>
///     Requests here carry no token: a probe from a load balancer or orchestrator has none.
/// </summary>
[Category("Integration")]
internal sealed class HealthEndpointTests : IntegrationTestsBase
{
    // The health endpoints read no module schema
    protected override Task MigrateAsync() => Task.CompletedTask;

    [Test]
    public async Task GetHealth_NoToken_ReturnsHealthy()
    {
        var response = await _client.GetAsync(HealthCheckExtensions.ReadinessRoute);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body, Is.EqualTo("Healthy"));
        });
    }

    [Test]
    public async Task GetAlive_NoToken_ReturnsHealthy()
    {
        var response = await _client.GetAsync(HealthCheckExtensions.LivenessRoute);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
