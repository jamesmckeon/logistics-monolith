using System.Net.Http.Headers;
using Throughline.Api.Tests.Common.Auth;
using Throughline.Common.Presentation.Http;

namespace Throughline.Api.Tests.Common;

/// <summary>
///     Base for every integration test class. Starts one test host and Postgres container per class, with
///     the auth settings every host needs plus any settings the class adds.
/// </summary>
internal abstract class IntegrationTestsBase
{
    public HttpClient _client;
    public TestFactory _testFactory;

    /// <summary>
    ///     Settings that let the host accept tokens from <see cref="TestTokens.Create" />. Every class
    ///     inherits them; override only to test a host that trusts a different issuer or key.
    /// </summary>
    protected virtual IDictionary<string, string> AuthSettings => TestTokens.HostSettings;

    /// <summary>
    ///     Settings specific to the class under test, e.g. a module's configuration. Empty by default.
    ///     These are applied on top of <see cref="AuthSettings" />.
    /// </summary>
    protected virtual IDictionary<string, string> AppSettings { get; } =
        new Dictionary<string, string>();

    /// <summary>
    ///     Applies the migrations for the module schemas the class uses.
    /// </summary>
    protected abstract Task MigrateAsync();

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var settings = new Dictionary<string, string>(AuthSettings);
        foreach (var setting in AppSettings)
        {
            settings[setting.Key] = setting.Value;
        }

        _testFactory = new TestFactory(settings);
        await _testFactory.InitializeAsync();
        _client = _testFactory.CreateClient();
        await MigrateAsync();
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

    /// <summary>
    ///     Builds a request that names an owner and carries a signed token for a caller holding the given
    ///     roles.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="uri">The request URI, relative to the test host.</param>
    /// <param name="ownerId">The owner the request acts for, sent in the <c>owner_id</c> header.</param>
    /// <param name="roles">
    ///     The caller's roles, written to the token.
    /// </param>
    /// <returns>The request. The caller disposes it and sets any content.</returns>
    protected static HttpRequestMessage CreateRequest(
        HttpMethod method, string uri, int ownerId, params string[] roles)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add(RequestContext.OwnerIdHeaderKey, ownerId.ToString());
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokens.Create(roles));

        return request;
    }
}
