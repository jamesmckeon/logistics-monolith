using Throughline.Common.Results;

namespace Throughline.Common.Tests.Results;

using TestResult = Result<string>;

[Category("Unit")]
public sealed class ResultTests
{
    private const string Value = "value";

    [Test]
    public void Success_ValidValue_SetsSuccessState()
    {
        var actual = TestResult.Success(Value);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.True);
            Assert.That(actual.Value, Is.EqualTo(Value));
            Assert.That(actual.Errors, Is.Empty);
            Assert.That(actual.ErrorType, Is.Null);
        });
    }

    [Test]
    public void ImplicitConversion_FromValue_ReturnsSuccessResult()
    {
        TestResult actual = Value;

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.True);
            Assert.That(actual.Value, Is.EqualTo(Value));
        });
    }

    #region Validation

    [Test]
    public void Validation_WithErrors_SetsValidationFailureState()
    {
        var errors = new[] { new Error("First", "fieldA"), new Error("Second") };

        var actual = TestResult.Validation(errors);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.ErrorType, Is.EqualTo(ErrorType.Validation));
            Assert.That(actual.Errors, Is.EqualTo(errors));
            Assert.That(actual.Value, Is.Null);
        });
    }

    [Test]
    public void Validation_WithErrorEnumerable_SetsValidationFailureState()
    {
        IEnumerable<Error> errors = new List<Error> { new("First") };

        var actual = TestResult.Validation(errors);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.ErrorType, Is.EqualTo(ErrorType.Validation));
            Assert.That(actual.Errors.Single().Description, Is.EqualTo("First"));
        });
    }

    [Test]
    public void Validation_WithStrings_CreatesFieldlessErrors()
    {
        var actual = TestResult.Validation("First", "Second");

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.ErrorType, Is.EqualTo(ErrorType.Validation));
            Assert.That(actual.Errors.Select(e => e.Description), Is.EqualTo(new[] { "First", "Second" }));
            Assert.That(actual.Errors.All(e => e.FieldName == null), Is.True);
        });
    }

    [Test]
    public void Validation_EmptyErrors_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => _ = TestResult.Validation(Array.Empty<Error>()));
        Assert.That(ex.ParamName, Is.EqualTo("errors"));
    }

    [Test]
    public void Validation_NullErrors_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => _ = TestResult.Validation((Error[])null!));
        Assert.That(ex.ParamName, Is.EqualTo("errors"));
    }

    #endregion

    #region Conflict

    [Test]
    public void Conflict_WithStrings_SetsConflictFailureState()
    {
        var actual = TestResult.Conflict("First", "Second");

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.ErrorType, Is.EqualTo(ErrorType.Conflict));
            Assert.That(actual.Errors.Select(e => e.Description), Is.EqualTo(new[] { "First", "Second" }));
            Assert.That(actual.Errors.All(e => e.FieldName == null), Is.True);
        });
    }

    [Test]
    public void Conflict_EmptyErrors_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => _ = TestResult.Conflict());
        Assert.That(ex.ParamName, Is.EqualTo("errors"));
    }

    #endregion
}
