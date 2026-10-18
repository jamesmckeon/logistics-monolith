using Azure.Core;

namespace Throughline.Simulator.Oms;

/// <summary>
///     One customer's Entra access token, reused until shortly before it expires
/// </summary>
internal sealed class AccessTokenSource
{
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(5);

    private readonly TokenCredential _credential;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TokenRequestContext _request;
    private readonly TimeProvider _time;
    private AccessToken? _token;

    public AccessTokenSource(TokenCredential credential, string scope, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        ArgumentNullException.ThrowIfNull(time);

        _credential = credential;
        _request = new TokenRequestContext([scope]);
        _time = time;
    }

    /// <param name="forceRefresh">True after Throughline rejected the current token</param>
    public async ValueTask<string> GetTokenAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        if (!forceRefresh && TryGetCurrent(out var current))
            return current;

        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (!forceRefresh && TryGetCurrent(out current))
                return current;

            var token = await _credential.GetTokenAsync(_request, cancellationToken);
            _token = token;
            return token.Token;
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool TryGetCurrent(out string token)
    {
        if (_token is { } current && current.ExpiresOn - _time.GetUtcNow() > RefreshMargin)
        {
            token = current.Token;
            return true;
        }

        token = "";
        return false;
    }
}
