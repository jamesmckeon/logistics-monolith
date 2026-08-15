using Throughline.Modules.Ordering.Domain.Models;

namespace Throughline.Modules.Ordering.Domain.Tests.Models;

[Category("Unit")]
public class PostalCodeTests
{
    [TestCase(" 05001 ")]
    [TestCase(" 05001-0001 ")]
    public void GetHashCode_ReturnsExpected(string value)
    {
        var left = new PostalCode(value);
        var right = new PostalCode(value.Trim());

        Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
    }

    [Test]
    public void Constructor_WithWhitespaces_TrimsWhitespace()
    {
        var sut = new PostalCode(" 05001 ");
        Assert.Multiple(() =>
        {
            Assert.That(sut.Value, Is.EqualTo("05001"));
            Assert.That(sut.BaseCode, Is.EqualTo("05001"));
        });
    }

    [Test]
    public void Constructor_Plus4WithWhitespaces_TrimsWhitespace()
    {
        var sut = new PostalCode(" 05001-0001 ");
        Assert.Multiple(() =>
        {
            Assert.That(sut.Value, Is.EqualTo("05001-0001"));
            Assert.That(sut.BaseCode, Is.EqualTo("05001"));
        });
    }

    #region IsValid

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("05000")]
    [TestCase("05001-0000")]
    [TestCase("99501")]
    [TestCase("99500-0000")]
    [TestCase("995000001")]
    public void IsValid_Invalid_ReturnsFalse(string? value)
    {
        Assert.That(PostalCode.IsValid(value!), Is.False);
    }


    [TestCase("05001")]
    [TestCase("05001-0001")]
    [TestCase("99500")]
    [TestCase("99500-9999")]
    public void IsValid_Valid_ReturnsTrue(string value)
    {
        Assert.That(PostalCode.IsValid(value), Is.True);
    }

    #endregion

    #region Equals

    [TestCase("05001", "05002")]
    [TestCase("05001", "05001-0001")]
    [TestCase("05001-0010", "05001-0001")]
    public void Equals_NotEqual_ReturnsFalse(string left, string right)
    {
        var leftCode = new PostalCode(left);
        var rightCode = new PostalCode(right);

        Assert.That(leftCode.Equals(rightCode), Is.False);
    }

    [TestCase("05001", "05001")]
    [TestCase("05001-0001", "05001-0001")]
    public void Equals_AreEqual_ReturnsTrue(string left, string right)
    {
        var leftCode = new PostalCode(left);
        var rightCode = new PostalCode(right);

        Assert.That(leftCode.Equals(rightCode), Is.True);
    }

    [TestCase("05001", "05002")]
    [TestCase("05001", "05001-0001")]
    [TestCase("05001-0010", "05001-0001")]
    public void Equals_ObjectsNotEqual_ReturnsFalse(string left, string right)
    {
        var leftCode = new PostalCode(left);
        var rightCode = new PostalCode(right);

        Assert.That(leftCode.Equals(rightCode as object), Is.False);
    }

    [TestCase("05001", "05001")]
    [TestCase("05001-0001", "05001-0001")]
    public void Equals_ObjectsAreEqual_ReturnsTrue(string left, string right)
    {
        var leftCode = new PostalCode(left);
        var rightCode = new PostalCode(right);

        Assert.That(leftCode.Equals(rightCode as object), Is.True);
    }

    #endregion

    #region Equals Operators

    [TestCase("05001", "05002")]
    [TestCase("05001", "05001-0001")]
    [TestCase("05001-0010", "05001-0001")]
    public void EqualsOperator_NotEqual_ReturnsFalse(string left, string right)
    {
        var leftCode = new PostalCode(left);
        var rightCode = new PostalCode(right);

        Assert.That(leftCode == rightCode, Is.False);
    }

    [TestCase("05001", "05001")]
    [TestCase("05001-0001", "05001-0001")]
    public void EqualsOperator_AreEqual_ReturnsTrue(string left, string right)
    {
        var leftCode = new PostalCode(left);
        var rightCode = new PostalCode(right);

        Assert.That(leftCode == rightCode, Is.True);
    }

    #endregion
}