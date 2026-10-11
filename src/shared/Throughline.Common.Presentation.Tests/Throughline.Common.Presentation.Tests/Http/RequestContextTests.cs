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
    public void ContainsHeader_NullContext_ReturnsFalse()
    {
        Accessor.Setup(s => s.HttpContext)
            .Returns(null as HttpContext);

        Assert.That(Sut.ContainsHeader("OwnerId"), Is.False);
    }

    [Test]
    public void ContainsHeader_HeaderMissing_ReturnsFalse()
    {
        var context = new DefaultHttpContext();
        Accessor.Setup(s => s.HttpContext)
            .Returns(context);

        Assert.That(Sut.ContainsHeader("OwnerId"), Is.False);
    }

    [TestCase("ownerId")]
    [TestCase("OWNERID")]
    public void ContainsHeader_HeaderExists_ReturnsTrue(string headerKey)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append(headerKey, "1");
        Accessor.Setup(s => s.HttpContext)
            .Returns(context);

        Assert.That(Sut.ContainsHeader("OwnerId"), Is.True);
    }
}