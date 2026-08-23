using Throughline.Common.Results;

namespace Throughline.Common.Tests.Results;

[Category("Unit")]
public sealed class ErrorTests
{
    [Test]
    public void IsRequired_Always_SetsFieldNameAndDescription()
    {
        var sut = Error.IsRequired("test");

        Assert.Multiple(() =>
        {
            Assert.That(sut.FieldName, Is.EqualTo("test"));
            Assert.That(sut.Description, Is.EqualTo("test is required."));
        });
    }

    [TestCase("")]
    [TestCase(" ")]
    public void Validation_DescriptionMissing_ThrowsArgumentException(string val)
    {
        var ex = Assert.Throws<ArgumentException>(() => _ = new Error(val));
        Assert.That(ex.ParamName, Is.EqualTo("description"));
    }

    [Test]
    public void Validation_NullDescription_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => _ = new Error(null!));
        Assert.That(ex.ParamName, Is.EqualTo("description"));
    }
}