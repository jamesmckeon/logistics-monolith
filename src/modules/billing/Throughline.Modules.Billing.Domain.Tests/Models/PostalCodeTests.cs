using Throughline.Common.Results;
using Throughline.Modules.Billing.Domain.Models;

namespace Throughline.Modules.Billing.Domain.Tests.Models;

[Category("Unit")]
public sealed class PostalCodeTests
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

    private static void AssertValidationError(Result<PostalCode> result, string expectedDescription)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.Errors.Select(e => e.Description), Has.Member(expectedDescription));
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.Validation));
        });
    }

    #region Create

    [TestCase("05001", "05001")]
    [TestCase("10000", "10000")]
    [TestCase("00501", "00501")]
    [TestCase("99500", "99500")]
    [TestCase("05001-0001", "05001-0001")]
    [TestCase(" 05001 ", "05001")]
    public void Create_ValidPostalCode_SucceedsWithTrimmedValue(string value, string expected)
    {
        var result = PostalCode.Create(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value!.Value, Is.EqualTo(expected));
        });
    }

    [Test]
    public void Create_Plus4_SetsBaseCodeToFiveDigitPrefix()
    {
        var result = PostalCode.Create("05001-0001");

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value!.BaseCode, Is.EqualTo("05001"));
        });
    }

    [TestCase("00500")]
    [TestCase("00100")]
    [TestCase("99501")]
    public void Create_StartOutsideServiceableRange_FailsValidation(string value)
    {
        var result = PostalCode.Create(value);

        AssertValidationError(result, "The start of a postal code must be between 00501 and 99500");
    }

    [Test]
    public void Create_PlusFourIsZero_FailsValidation()
    {
        var result = PostalCode.Create("05001-0000");

        AssertValidationError(result, "The last four of a postal code must be greater than 0000");
    }

    [TestCase("123")]
    [TestCase("1234567")]
    [TestCase("00501-1")]
    [TestCase("not-a-zip")]
    [TestCase("abcde")]
    public void Create_MalformedFormat_FailsValidation(string value)
    {
        var result = PostalCode.Create(value);

        AssertValidationError(result, "Invalid postal code format");
    }

    [Test]
    public void Create_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => PostalCode.Create(null!));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_EmptyOrWhitespace_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => PostalCode.Create(value));
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