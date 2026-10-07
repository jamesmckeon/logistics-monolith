using Microsoft.EntityFrameworkCore;

namespace Throughline.Api.Tests.Common;

internal abstract class IntegrationTestsBase<T>
    where T : DbContext
{
    public HttpClient _client;
    public TestFactory<T> _testFactory;

    protected virtual IDictionary<string, string> AppSettings { get; } =
        new Dictionary<string, string>();

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _testFactory = new TestFactory<T>(AppSettings.AsReadOnly());
        await _testFactory.InitializeAsync();
        _client = _testFactory.CreateClient();
        await _testFactory.MigrateDbAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _client?.Dispose();
        if (_testFactory is not null)
        {
            await _testFactory.DisposeAsync();
        }
    }
}