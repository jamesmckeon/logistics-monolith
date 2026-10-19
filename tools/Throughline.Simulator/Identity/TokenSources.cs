using System.Collections.Concurrent;
using Azure.Identity;
using Microsoft.Extensions.Options;
using Throughline.Simulator.Configuration;
using Throughline.Simulator.Oms;

namespace Throughline.Simulator.Identity;

/// <summary>
///     One cached token source per caller, kept for the process's life so a token outlives the HTTP
///     handlers that use it. Customers' OMS applications sign in with client credentials; simulated
///     staff sign in as users with the password grant.
/// </summary>
internal sealed class TokenSources
{
    public const string EntraHttpClient = "entra";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SimulatorOptions _options;
    private readonly ConcurrentDictionary<string, AccessTokenSource> _sources = new();
    private readonly TimeProvider _time;

    public TokenSources(IOptions<SimulatorOptions> options, IHttpClientFactory httpClientFactory, TimeProvider time)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _time = time;
    }

    public AccessTokenSource ForCustomer(string customer) =>
        _sources.GetOrAdd($"customer:{customer}", _ =>
        {
            var profile = _options.Customers[customer];

            return new AccessTokenSource(
                new ClientSecretCredential(_options.TenantId, profile.OmsClientId, profile.OmsClientSecret),
                _options.ApiScope,
                _time);
        });

    public AccessTokenSource ForStaff(string username) =>
        _sources.GetOrAdd($"staff:{username}", _ =>
        {
            var facility = _options.Facility;
            var staff = facility.Receivers.Single(r => r.Username == username);

            return new AccessTokenSource(
                new PasswordGrantCredential(
                    _httpClientFactory.CreateClient(EntraHttpClient),
                    facility.UserTenantId,
                    facility.UserClientId,
                    staff.Username,
                    staff.Password,
                    _time),
                _options.ApiScope,
                _time);
        });
}
