using Microsoft.AspNetCore.Http;

namespace Throughline.Common.Presentation.Http;

public sealed class RequestContext : IRequestContext
{
    public const string OwnerIdHeaderKey = "owner_id";

    /// <summary>
    ///     The name the token's <c>oid</c> claim has on the request's user: the JWT bearer handler maps inbound claim
    ///     types by default, and the API doesn't turn that off
    /// </summary>
    public const string ObjectIdClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    private readonly IHttpContextAccessor _accessor;

    public RequestContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public int OwnerId =>
        int.Parse(GetFirstHeaderValue(OwnerIdHeaderKey));

    /// <summary>
    ///     The Entra object ID (<c>oid</c>) of the authenticated caller: a user's object ID for a person, the service
    ///     principal's for a service
    /// </summary>
    /// <exception cref="InvalidOperationException">There's no request, or its token has no valid <c>oid</c></exception>
    public Guid CallerId
    {
        get
        {
            if (_accessor.HttpContext == null)
            {
                throw new InvalidOperationException("No active HttpContext.");
            }

            var user = _accessor.HttpContext.User;
            var value = user.FindFirst(ObjectIdClaimType)?.Value;

            if (!Guid.TryParse(value, out var callerId))
            {
                throw new InvalidOperationException("The caller's token has no valid object ID (oid) claim.");
            }

            return callerId;
        }
    }

    public string? GetHeaderValue(string header)
    {
        if (!ContainsHeader(header))
        {
            return null;
        }

        return _accessor.HttpContext?.Request.Headers[header];
    }

    private string GetFirstHeaderValue(string key)
    {
        if (_accessor.HttpContext == null)
        {
            throw new InvalidOperationException("No active HttpContext.");
        }

        if (!ContainsHeader(key))
        {
            throw new InvalidOperationException($"No header with key '{key}' found.");
        }

        var value = _accessor.HttpContext.Request.Headers[key].FirstOrDefault();
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Header '{key}' has no value.");
        }

        return value;
    }

    public bool ContainsHeader(string header)
    {
        if (_accessor.HttpContext == null)
        {
            return false;
        }

        return _accessor.HttpContext.Request.Headers.ContainsKey(header);
    }
}