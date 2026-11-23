using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using Throughline.Common.Presentation.Http;

namespace Throughline.Common.Presentation.Tests.Http;

[Category("Unit")]
internal sealed class RequestContextTests
{
    private Mock<IHttpContextAccessor> Accessor { get; set; }
    private RequestContext Sut { get; set; }

    [SetUp]
    public void Setup()
    {
        Accessor = new();
        Sut = new(Accessor.Object);
    }

    [Test]
    public void GetHeaderValue_NullContext_ReturnsNull()
    {
        Accessor.Setup(s => s.HttpContext)
            .Returns(null as HttpContext);

        Assert.That(Sut.GetHeaderValue("TestHeader"), Is.Null);
    }

    [Test]
    public void GetHeaderValue_HeaderMissing_ReturnsNull()
    {
        var context = new DefaultHttpContext();
        Accessor.Setup(s => s.HttpContext)
            .Returns(context);

        Assert.That(Sut.GetHeaderValue("TestHeader"), Is.Null);
    }

    [TestCase("testheader", "1")]
    [TestCase("TESTHEADER", "1")]
    public void GetHeaderValue_HeaderExists_ReturnsExpectedValue(string headerKey, string value)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append(headerKey, value);
        Accessor.Setup(s => s.HttpContext)
            .Returns(context);

        Assert.That(Sut.GetHeaderValue("TestHeader"), Is.EqualTo(value));
    }

    #region OwnerId

    [Test]
    public void OwnerId_HeaderPresent_ReturnsHeaderValue()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append(RequestContext.OwnerIdHeaderKey, "7");
        Accessor.Setup(s => s.HttpContext)
            .Returns(context);

        Assert.That(Sut.OwnerId, Is.EqualTo(7));
    }

    [Test]
    public void OwnerId_HeaderHasSeveralValues_ReturnsFirstValue()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append(RequestContext.OwnerIdHeaderKey, new StringValues(["7", "9"]));
        Accessor.Setup(s => s.HttpContext)
            .Returns(context);

        Assert.That(Sut.OwnerId, Is.EqualTo(7));
    }

    [Test]
    public void OwnerId_NullContext_ThrowsInvalidOperationException()
    {
        Accessor.Setup(s => s.HttpContext)
            .Returns(null as HttpContext);

        Assert.That(() => Sut.OwnerId, Throws.InvalidOperationException);
    }

    [Test]
    public void OwnerId_HeaderMissing_ThrowsInvalidOperationException()
    {
        var context = new DefaultHttpContext();
        Accessor.Setup(s => s.HttpContext)
            .Returns(context);

        Assert.That(() => Sut.OwnerId, Throws.InvalidOperationException);
    }

    [Test]
    public void OwnerId_HeaderHasNoValue_ThrowsInvalidOperationException()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append(RequestContext.OwnerIdHeaderKey, string.Empty);
        Accessor.Setup(s => s.HttpContext)
            .Returns(context);

        Assert.That(() => Sut.OwnerId, Throws.InvalidOperationException);
    }

    #endregion

    #region CallerId

    [Test]
    public void CallerId_ObjectIdClaimPresent_ReturnsObjectId()
    {
        var objectId = Guid.Parse("5d0c6e2a-3f1b-4a7c-8e94-b2a1c3d4e5f6");
        Accessor.Setup(s => s.HttpContext)
            .Returns(ContextWithClaim(objectId.ToString()));

        Assert.That(Sut.CallerId, Is.EqualTo(objectId));
    }

    [Test]
    public void CallerId_NullContext_ThrowsInvalidOperationException()
    {
        Accessor.Setup(s => s.HttpContext)
            .Returns(null as HttpContext);

        Assert.That(() => Sut.CallerId, Throws.InvalidOperationException);
    }

    [Test]
    public void CallerId_ObjectIdClaimMissing_ThrowsInvalidOperationException()
    {
        Accessor.Setup(s => s.HttpContext)
            .Returns(new DefaultHttpContext());

        Assert.That(() => Sut.CallerId, Throws.InvalidOperationException);
    }

    [Test]
    public void CallerId_ObjectIdClaimNotAGuid_ThrowsInvalidOperationException()
    {
        Accessor.Setup(s => s.HttpContext)
            .Returns(ContextWithClaim("not-a-guid"));

        Assert.That(() => Sut.CallerId, Throws.InvalidOperationException);
    }

    #endregion

    private static DefaultHttpContext ContextWithClaim(string objectId) =>
        new() { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(RequestContext.ObjectIdClaimType, objectId)])) };
}