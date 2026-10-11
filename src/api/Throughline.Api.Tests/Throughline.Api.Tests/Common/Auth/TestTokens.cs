using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Throughline.Api.Tests.Common.Auth;

internal static class TestTokens
{
    public const string Issuer = "throughline-tests";
    public const string Audience = "throughline-api";

    public static readonly byte[] SigningKey = RandomNumberGenerator.GetBytes(32);
    public static readonly string SigningKeyBase64 = Convert.ToBase64String(SigningKey);

    /// <summary>
    ///     Creates a signed access token for a test caller holding the given app roles. The token is
    ///     issued by <see cref="Issuer" /> for <see cref="Audience" />, signed with
    ///     <see cref="SigningKey" />, and expires after five minutes.
    /// </summary>
    /// <param name="roles">
    ///     The caller's app roles, written to the <c>roles</c> claim as Entra does. Pass functional roles
    ///     and owner grants together, e.g. <c>"OrderIntakeIntegration", "Owner.1"</c>. Pass none for an
    ///     authenticated caller who holds no roles.
    /// </param>
    /// <returns>The token, for use as the value of a <c>Bearer</c> Authorization header.</returns>
    public static string Create(params string[] roles)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object> { ["roles"] = roles },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(SigningKey),
                SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}