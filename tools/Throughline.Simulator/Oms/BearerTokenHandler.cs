using System.Net;
using System.Net.Http.Headers;

namespace Throughline.Simulator.Oms;

/// <summary>
///     Signs each request with the customer's token. On a 401 it fetches a new token and tries once
///     more, as a real client would after its token was revoked or expired early.
/// </summary>
internal sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly AccessTokenSource _tokens;

    public BearerTokenHandler(AccessTokenSource tokens)
    {
        _tokens = tokens;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", await _tokens.GetTokenAsync(false, cancellationToken));

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        response.Dispose();
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", await _tokens.GetTokenAsync(true, cancellationToken));

        return await base.SendAsync(request, cancellationToken);
    }
}
