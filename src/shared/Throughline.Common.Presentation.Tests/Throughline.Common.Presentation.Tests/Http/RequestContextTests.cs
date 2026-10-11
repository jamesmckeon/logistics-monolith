using Microsoft.AspNetCore.Http;
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

        Assert.That(Sut.GetHeaderValue("OwnerId"), Is.Null);
    }

    [Test]
    public void GetHeaderValue_HeaderMissing_ReturnsNull()
    {
        var context = new DefaultHttpContext();
        Accessor.Setup(s => s.HttpContext)
            .Returns(context);

        Assert.That(Sut.GetHeaderValue("OwnerId"), Is.Null);
    }

    [TestCase("ownerId", "1")]
    [TestCase("OWNERID", "1")]
    public void GetHeaderValue_HeaderExists_ReturnsExpectedValue(string headerKey, string value)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append(headerKey, value);
        Accessor.Setup(s => s.HttpContext)
            .Returns(context);

        Assert.That(Sut.GetHeaderValue("OwnerId"), Is.EqualTo(value));
    }
}