using Microsoft.EntityFrameworkCore;

namespace Throughline.Api.Tests.Common;

internal abstract class IntegrationTestsBase<T> where T : DbContext
{
    public HttpClient _client;
    public TestFactory<T> _testFactory;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _testFactory = new();
        await _testFactory.InitializeAsync();
        _client = _testFactory.CreateClient();
        await _testFactory.ApplyMigrationsAsync();
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