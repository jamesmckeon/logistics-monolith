using Microsoft.Extensions.DependencyInjection;
using Throughline.Api.Health;

namespace Throughline.Api.Tests.Health;

[Category("Unit")]
internal sealed class HealthCheckExtensionsTests
{
    [TestCase("")]
    [TestCase("  ")]
    public void AddThroughlineHealthChecks_BlankConnectionString_Throws(string connectionString)
    {
        var services = new ServiceCollection();

        Assert.That(
            () => services.AddThroughlineHealthChecks(connectionString),
            Throws.ArgumentException);
    }
}
