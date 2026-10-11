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
}