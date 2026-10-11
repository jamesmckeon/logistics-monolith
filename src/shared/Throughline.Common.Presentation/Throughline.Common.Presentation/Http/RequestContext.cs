using Microsoft.AspNetCore.Http;

namespace Throughline.Common.Presentation.Http;

public sealed class RequestContext : IRequestContext
{
    public const string OwnerIdHeaderKey = "owner_id";

    private readonly IHttpContextAccessor _accessor;

    public RequestContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public int OwnerId =>
        int.Parse(GetFirstHeaderValue(OwnerIdHeaderKey));

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