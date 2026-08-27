using Throughline.Common.Results;
using Throughline.Modules.Ordering.Domain.Models;

namespace Throughline.Modules.Ordering.Domain.Tests.Models;

[Category("Unit")]
public sealed class AddressStateTests
{
    private static void AssertValidationError(Result<AddressState> result, string expectedDescription)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors.Select(e => e.Description), Has.Member(expectedDescription));
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.Validation));
        });
    }

    #region Create

    [TestCase("IL", "IL")]
    [TestCase("il", "IL")]
    [TestCase("Ca", "CA")]
    public void Create_TwoAlphaCharacters_SucceedsUppercased(string value, string expected)
    {
        var result = AddressState.Create(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value!.Value, Is.EqualTo(expected));
        });
    }

    [TestCase(" il ", "IL")]
    [TestCase("  CA", "CA")]
    public void Create_SurroundingWhitespace_SucceedsTrimmedAndUppercased(string value, string expected)
    {
        var result = AddressState.Create(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value!.Value, Is.EqualTo(expected));
        });
    }

    [TestCase("I")]
    [TestCase("ILL")]
    [TestCase("I1")]
    [TestCase("12")]
    [TestCase("I.")]
    public void Create_NotTwoAlphaCharacters_FailsValidation(string value)
    {
        var result = AddressState.Create(value);

        AssertValidationError(result, "state must be 2 alpha characters");
    }

    [Test]
    public void Create_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => AddressState.Create(null!));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_EmptyOrWhitespace_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => AddressState.Create(value));
    }

    #endregion

    #region Equals

    [Test]
    public void Equals_SameValue_ReturnsTrue()
    {
        var left = new AddressState("IL");
        var right = new AddressState("IL");

        Assert.Multiple(() =>
        {
            Assert.That(left.Equals(right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        });
    }

    [Test]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        var left = new AddressState("IL");
        var right = new AddressState("CA");

        Assert.That(left == right, Is.False);
    }

    #endregion
}