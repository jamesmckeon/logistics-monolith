using Microsoft.EntityFrameworkCore;

namespace Throughline.Api.Tests.Common;

/// <summary>
///     Base for a test class that exercises a single module: migrates that module's schema only.
/// </summary>
internal abstract class IntegrationTestsBase<T> : IntegrationTestsBase
    where T : DbContext
{
    protected override Task MigrateAsync() => _testFactory.MigrateAsync<T>();
}
