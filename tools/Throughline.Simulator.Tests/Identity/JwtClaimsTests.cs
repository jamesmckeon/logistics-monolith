using System.Text;
using Throughline.Simulator.Identity;

namespace Throughline.Simulator.Tests.Identity;

[Category("Unit")]
public sealed class JwtClaimsTests
{
    private static readonly Guid ObjectId = Guid.Parse("dfa48049-b666-437e-80bc-4dd3a5960e55");

    [Test]
    public void ObjectId_TokenWithOid_ReturnsIt()
    {
        var token = Token($$"""{"oid":"{{ObjectId}}","name":"Receiver 1 (simulated)"}""");

        var actual = JwtClaims.ObjectId(token);

        Assert.That(actual, Is.EqualTo(ObjectId));
    }

    [Test]
    public void ObjectId_TokenWithoutOid_ThrowsFormatException()
    {
        var token = Token("""{"name":"Receiver 1 (simulated)"}""");

        Assert.Throws<FormatException>(() => JwtClaims.ObjectId(token));
    }

    // an unsigned token: only the payload matters here
    private static string Token(string payloadJson) =>
        $"{Base64Url("""{"alg":"none"}""")}.{Base64Url(payloadJson)}.signature";

    private static string Base64Url(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
