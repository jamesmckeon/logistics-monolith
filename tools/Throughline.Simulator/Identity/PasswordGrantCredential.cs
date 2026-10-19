using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Azure.Core;

namespace Throughline.Simulator.Identity;

/// <summary>
///     Signs a simulated member of staff in with their username and password (the OAuth 2.0 password
///     grant). Microsoft discourages this flow because it can't do MFA; it's used here only for test
///     users in a separate tenant (docs/design/simulator.md, decision 9).
/// </summary>
internal sealed class PasswordGrantCredential : TokenCredential
{
    private readonly string _clientId;
    private readonly HttpClient _http;
    private readonly string _password;
    private readonly string _tenantId;
    private readonly TimeProvider _time;
    private readonly string _username;

    public PasswordGrantCredential(
        HttpClient http, string tenantId, string clientId, string username, string password, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentNullException.ThrowIfNull(time);

        _http = http;
        _tenantId = tenantId;
        _clientId = clientId;
        _username = username;
        _password = password;
        _time = time;
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
        GetTokenAsync(requestContext, cancellationToken).AsTask().GetAwaiter().GetResult();

    public override async ValueTask<AccessToken> GetTokenAsync(
        TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _clientId,
            ["username"] = _username,
            ["password"] = _password,
            ["scope"] = string.Join(' ', requestContext.Scopes)
        });

        using var response = await _http.PostAsync(
            new Uri($"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token"), content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // the error description carries the AADSTS code; never the password
            var error = await response.Content.ReadFromJsonAsync<TokenError>(cancellationToken);
            throw new InvalidOperationException(
                $"Sign-in failed for {_username}: {error?.Description ?? response.StatusCode.ToString()}");
        }

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);

        if (token is null || string.IsNullOrEmpty(token.AccessToken))
            throw new InvalidOperationException($"Sign-in for {_username} returned no access token");

        return new AccessToken(token.AccessToken, _time.GetUtcNow().AddSeconds(token.ExpiresIn));
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private sealed record TokenError([property: JsonPropertyName("error_description")] string? Description);
}
