using Throughline.Common.Results;

namespace Throughline.Common.Tests.Results;

[Category("Unit")]
public sealed class ErrorTests
{
    #region Constructor

    [TestCase("")]
    [TestCase(" ")]
    public void Constructor_DescriptionMissing_ThrowsArgumentException(string val)
    {
        var ex = Assert.Throws<ArgumentException>(() => _ = new Error(val));
        Assert.That(ex.ParamName, Is.EqualTo("description"));
    }

    [Test]
    public void Constructor_NullDescription_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => _ = new Error(null!));
        Assert.That(ex.ParamName, Is.EqualTo("description"));
    }

    [Test]
    public void Constructor_WithFieldName_SetsDescriptionAndFieldName()
    {
        var sut = new Error("Quantity must be positive", "quantity");

        Assert.Multiple(() =>
        {
            Assert.That(sut.Description, Is.EqualTo("Quantity must be positive"));
            Assert.That(sut.FieldName, Is.EqualTo("quantity"));
        });
    }

    [Test]
    public void Constructor_WithoutFieldName_LeavesFieldNameNull()
    {
        var sut = new Error("Something went wrong");

        Assert.Multiple(() =>
        {
            Assert.That(sut.Description, Is.EqualTo("Something went wrong"));
            Assert.That(sut.FieldName, Is.Null);
        });
    }

    #endregion

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
}
