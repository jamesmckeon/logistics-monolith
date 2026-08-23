using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Throughline.Common.Results;

namespace Throughline.Common.Presentation.Tests;

using TestResult = Result<object>;

[Category("Unit")]
public sealed class ResultProblemMapperTests
{
    #region ToProblemDetails

    [Test]
    public void ToProblemDetails_SuccessResult_ThrowsExpected()
    {
        var result = TestResult.Success(new object());
        var ex = Assert.Throws<InvalidOperationException>(() => result.ToProblemDetails());

        Assert.That(ex.Message,
            Is.EqualTo("A ProblemDetails instance cannot be constructed from a successful result"));
    }

    [Test]
    public void ToProblemDetails_NullResult_Throws()
    {
        TestResult result = null!;
        Assert.That(() => result.ToProblemDetails(), Throws.ArgumentNullException);
    }


    [Test]
    public void ToProblemDetails_ValidationWithFieldErrors_ReturnsValidationProblemDetail()
    {
        var errors = new Error[]
        {
            new("Test Error 1", "Field 1"),
            new("Test Error 2", "Field 2")
        };

        var result = TestResult.Validation(errors);
        var actual = (ValidationProblemDetails)result.ToProblemDetails();

        Assert.Multiple(() =>
        {
            Assert.That(actual.Title, Is.EqualTo("One or more validation errors occurred"));
            Assert.That(actual.Status, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(actual.Detail, Is.Null);
            Assert.That(actual.Errors.Count, Is.EqualTo(2));
            AssertFieldErrors(actual, errors[0]);
            AssertFieldErrors(actual, errors[1]);
        });
    }

    [TestCase("Test Error 1")]
    [TestCase("Test Error 1", "Test Error 2")]
    public void ToProblemDetails_ValidationMixedErrorCounts_ReturnsExpected(params string[] errors)
    {
        var expectedDetail = errors.Length == 1 ? errors[0] : string.Join("; ", errors);
        var result = TestResult.Validation(errors);
        var actual = result.ToProblemDetails();

        Assert.Multiple(() =>
        {
            Assert.That(actual.Title, Is.EqualTo("One or more validation errors occurred"));
            Assert.That(actual.Status, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(actual.Detail, Is.EqualTo(expectedDetail));
            Assert.That(actual.Extensions.Count, Is.EqualTo(0));
        });
    }

    [Test]
    public void ToProblemDetails_ValidationMixedFieldAndNonFieldErrors_ReturnsExpected()
    {
        Error[] errors =
        [
            new("Test Error 1"),
            new("Test Error 2", "TestField")
        ];

        var expectedDetail = "One or more fields are invalid; Test Error 1";
        var result = TestResult.Validation(errors);
        var actual = result.ToProblemDetails();

        Assert.Multiple(() =>
        {
            Assert.That(actual.Title, Is.EqualTo("One or more validation errors occurred"));
            Assert.That(actual.Status, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(actual.Detail, Is.EqualTo(expectedDetail));
            Assert.That(actual.Extensions.Count, Is.EqualTo(0));
        });
    }

    [TestCase("Test Error 1")]
    [TestCase("Test Error 1", "Test Error 2")]
    public void ToProblemDetails_ConflictMixedErrorCounts_ReturnsExpected(params string[] errors)
    {
        var expectedDetail = errors.Length == 1 ? errors[0] : string.Join("; ", errors);
        var result = TestResult.Conflict(errors.ToArray());
        var actual = result.ToProblemDetails();

        Assert.Multiple(() =>
        {
            Assert.That(actual.Title, Is.EqualTo("A conflict occurred"));
            Assert.That(actual.Status, Is.EqualTo(StatusCodes.Status409Conflict));
            Assert.That(actual.Detail, Is.EqualTo(expectedDetail));
            Assert.That(actual.Extensions.Count, Is.EqualTo(0));
        });
    }

    #endregion

    private static void AssertFieldErrors(ValidationProblemDetails actual, Error error)
    {
        Assert.That(actual.Errors.Any(s => s.Key == error.FieldName &&
                                           s.Value.Single() == error.Description), Is.True);
    }
}