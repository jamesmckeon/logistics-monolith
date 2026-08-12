using Throughline.Modules.Ordering.Domain.Models;

namespace Throughline.Modules.Ordering.Domain.Tests.Models;

[Category("Unit")]
public sealed class PostalZoneTests
{
    [Test]
    public void Includes_NullZone_ThrowsExpected()
    {
        var sut = new PostalZone(new PostalCode("05001"), new PostalCode("05001"));
        var ex = Assert.Throws<ArgumentNullException>(() => sut.Includes(null!));

        Assert.That(ex.ParamName, Is.EqualTo("postalCode"));
    }

    [TestCase("05001", "05001", "05002")]
    [TestCase("05002", "05003", "05001")]
    [TestCase("05002", "05004", "05005")]
    public void Includes_CodeNotInZone_ReturnsFalse(string start, string end, string code)
    {
        var sut = new PostalZone(new PostalCode(start), new PostalCode(end));
        Assert.That(sut.Includes(new PostalCode(code)), Is.False);
    }

    [TestCase("05001", "05001", "05001")]
    [TestCase("05001", "05001", "05001")]
    [TestCase("05002", "05003", "05002")]
    [TestCase("05002", "05003", "05003")]
    [TestCase("05002", "05004", "05003")]
    public void Includes_CodeInZone_ReturnsTrue(string start, string end, string code)
    {
        var sut = new PostalZone(new PostalCode(start), new PostalCode(end));
        Assert.That(sut.Includes(new PostalCode(code)), Is.True);
    }

    [Test]
    public void Constructor_StartAfterEnd_ThrowsExpected()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _ = new PostalZone(new PostalCode("05002"), new PostalCode("05001")));

        Assert.That(ex.Message, Is.EqualTo("Start must be lesser or equal to end"));
    }
}