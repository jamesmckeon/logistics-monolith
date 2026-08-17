using Throughline.Common.Results;

namespace Throughline.Common.Tests.Results;

[Category("Unit")]
public sealed class ErrorTests
{
    #region Factory methods

    [Test]
    public void Validation_Always_SetsValidationTypeAndDescription()
    {
        var sut = Error.Validation("test");

        Assert.Multiple(() =>
        {
            Assert.That(sut.ErrorType, Is.EqualTo(ErrorType.Validation));
            Assert.That(sut.Description, Is.EqualTo("test"));
        });
    }

    [Test]
    public void Unavailable_Always_SetsUnavailableTypeAndDescription()
    {
        var sut = Error.Unavailable("test");

        Assert.Multiple(() =>
        {
            Assert.That(sut.ErrorType, Is.EqualTo(ErrorType.Unavailable));
            Assert.That(sut.Description, Is.EqualTo("test"));
        });
    }

    [Test]
    public void Conflict_Always_SetsConflictTypeAndDescription()
    {
        var sut = Error.Conflict("test");

        Assert.Multiple(() =>
        {
            Assert.That(sut.ErrorType, Is.EqualTo(ErrorType.Conflict));
            Assert.That(sut.Description, Is.EqualTo("test"));
        });
    }

    #endregion

    #region Constructor validation

    [Test]
    public void Validation_NullDescription_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Error.Validation(null!));
        Assert.That(ex.ParamName, Is.EqualTo("description"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Validation_BlankDescription_ThrowsArgumentException(string description)
    {
        var ex = Assert.Throws<ArgumentException>(() => Error.Validation(description));
        Assert.That(ex.ParamName, Is.EqualTo("description"));
    }

    #endregion

    #region Equals

    [Test]
    public void Equals_NullOther_ReturnsFalse()
    {
        var sut = Error.Validation("test");
        Assert.That(sut.Equals(null!), Is.False);
    }

    [Test]
    public void Equals_SameInstance_ReturnsTrue()
    {
        var sut = Error.Validation("test");
        Assert.That(sut.Equals(sut), Is.True);
    }

    [Test]
    public void Equals_DifferentErrorType_ReturnsFalse()
    {
        var sut = Error.Validation("test");
        var other = Error.Conflict("test");

        Assert.That(sut.Equals(other), Is.False);
    }

    [Test]
    public void Equals_DifferentMessage_ReturnsFalse()
    {
        var sut = Error.Validation("test");
        var other = Error.Validation("testt");

        Assert.That(sut.Equals(other), Is.False);
    }

    [Test]
    public void Equals_SameTypeAndMessage_ReturnsTrue()
    {
        var sut = Error.Validation("test");
        var other = Error.Validation("test");

        Assert.That(sut.Equals(other), Is.True);
    }

    [Test]
    public void Equals_NullObject_ReturnsFalse()
    {
        var sut = Error.Validation("test");
        Assert.That(sut.Equals((object?)null), Is.False);
    }

    [Test]
    public void Equals_NonErrorObject_ReturnsFalse()
    {
        var sut = Error.Validation("test");
        Assert.That(sut.Equals("test"), Is.False);
    }

    [Test]
    public void Equals_BoxedEqualError_ReturnsTrue()
    {
        var sut = Error.Validation("test");
        object other = Error.Validation("test");

        Assert.That(sut.Equals(other), Is.True);
    }

    #endregion

    #region GetHashCode

    [Test]
    public void GetHashCode_EqualErrors_ReturnSameValue()
    {
        var first = Error.Validation("test");
        var second = Error.Validation("test");

        Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
    }

    [Test]
    public void GetHashCode_DifferentErrors_ReturnDifferentValues()
    {
        var first = Error.Validation("test");
        var second = Error.Conflict("test");

        Assert.That(first.GetHashCode(), Is.Not.EqualTo(second.GetHashCode()));
    }

    #endregion

    #region Equality operators

    [TestCaseSource(nameof(EqualityCases))]
    public void OperatorEquals_Cases_ReturnsExpected(Error? left, Error? right, bool expected)
    {
        Assert.That(left == right, Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(EqualityCases))]
    public void OperatorNotEquals_Cases_ReturnsNegationOfEquals(Error? left, Error? right, bool equalExpected)
    {
        Assert.That(left != right, Is.EqualTo(!equalExpected));
    }

    private static IEnumerable<TestCaseData> EqualityCases()
    {
        yield return new TestCaseData(null, null, true);
        yield return new TestCaseData(Error.Validation("test"), null, false);
        yield return new TestCaseData(null, Error.Validation("test"), false);
        yield return new TestCaseData(Error.Validation("test"), Error.Validation("test"), true);
        yield return new TestCaseData(Error.Validation("test"), Error.Conflict("test"), false);
    }

    #endregion
}
