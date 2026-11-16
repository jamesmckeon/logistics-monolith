using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace Throughline.SmokeTests;

[SetUpFixture]
public sealed class SetupFixture
{
    private static HttpClient? _client;

    internal static HttpClient Client =>
        _client ?? throw new InvalidOperationException("Client has not been setup yet");

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<SetupFixture>()
            .AddEnvironmentVariables("SMOKE_") // for CI pipeline
            .Build();

        var settings = config.Get<SmokeTestSettings>();

        if (settings == null)
        {
            throw new InvalidOperationException("One or more configuration settings are missing");
        }

        if (string.IsNullOrWhiteSpace(settings.ClientId))
        {
            throw new InvalidOperationException("ClientId setting is blank or whitespace");
        }

        if (string.IsNullOrWhiteSpace(settings.TenantId))
        {
            throw new InvalidOperationException("TenantId setting is blank or whitespace");
        }

        if (string.IsNullOrWhiteSpace(settings.ClientSecret))
        {
            throw new InvalidOperationException("ClientSecret setting is blank or whitespace");
        }

        if (string.IsNullOrWhiteSpace(settings.Scope))
        {
            throw new InvalidOperationException("Scope setting is blank or whitespace");
        }

        var credential = new ClientSecretCredential(
            settings.TenantId, settings.ClientId, settings.ClientSecret);

        var token = await credential.GetTokenAsync(
            new TokenRequestContext([settings.Scope],
                null));

        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        };

        _client = new HttpClient(handler);
        _client.BaseAddress = settings.BaseUrl;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", token.Token);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        if (_client != null)
        {
            _client.Dispose();
        }
    }
}