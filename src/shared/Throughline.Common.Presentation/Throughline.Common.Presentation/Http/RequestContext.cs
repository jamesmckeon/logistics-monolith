using Microsoft.AspNetCore.Http;

namespace Throughline.Common.Presentation.Http;

public sealed class RequestContext
{
    private readonly IHttpContextAccessor _accessor;

    public RequestContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public int OwnerId =>
        int.Parse(GetFirstHeaderValue("owner_id"));

    private string GetFirstHeaderValue(string key)
    {
        if (_accessor.HttpContext == null)
            throw new InvalidOperationException("No active HttpContext.");

        if (!_accessor.HttpContext.Request.Headers.ContainsKey(key))
            throw new InvalidOperationException($"No header with key '{key}' found.");

        var value = _accessor.HttpContext.Request.Headers[key].FirstOrDefault();
        if (string.IsNullOrEmpty(value))
            throw new InvalidOperationException($"Header '{key}' has no value.");

        return value;
    }
}