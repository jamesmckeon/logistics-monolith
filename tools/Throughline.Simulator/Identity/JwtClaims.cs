using System.Text;
using System.Text.Json;

namespace Throughline.Simulator.Identity;

/// <summary>
///     Reads claims from an access token the simulator was issued. It doesn't validate the token;
///     Throughline does that.
/// </summary>
internal static class JwtClaims
{
    /// <summary>
    ///     The signed-in user's object id (<c>oid</c>), the stable id Entra gives each user
    /// </summary>
    public static Guid ObjectId(string accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var parts = accessToken.Split('.');

        if (parts.Length != 3)
            throw new FormatException("An access token has three dot-separated parts");

        using var payload = JsonDocument.Parse(DecodeBase64Url(parts[1]));

        if (!payload.RootElement.TryGetProperty("oid", out var oid) || !Guid.TryParse(oid.GetString(), out var id))
            throw new FormatException("The access token has no oid claim");

        return id;
    }

    private static string DecodeBase64Url(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }
}
