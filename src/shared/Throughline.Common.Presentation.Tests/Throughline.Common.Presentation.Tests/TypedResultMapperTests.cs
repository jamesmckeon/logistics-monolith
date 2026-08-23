using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Throughline.Common.Results;

namespace Throughline.Common.Presentation.Tests;

using TestResult = Result<object>;

[Category("Unit")]
public sealed class TypedResultMapperTests
{
    private static readonly object SuccessValue = new();

    #region ToTypedResult

    [Test]
    public void ToTypedResult_NullResult_Throws()
    {
        TestResult result = null!;
        Assert.That(() => result.ToTypedResult(), Throws.ArgumentNullException);
    }

    [Test]
    public void ToTypedResult_SuccessResult_ReturnsOkWithValue()
    {
        var result = TestResult.Success(SuccessValue);

        var actual = result.ToTypedResult() as Ok<object>;

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            Assert.That(actual.Value, Is.SameAs(SuccessValue));
        });
    }

    [Test]
    public void ToTypedResult_ValidationResult_ReturnsBadRequestWithProblem()
    {
        var result = TestResult.Validation(["Bad input"]);

        var actual = result.ToTypedResult() as BadRequest<ProblemDetails>;

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(actual.Value, Is.Not.Null);
            Assert.That(actual.Value!.Title, Is.EqualTo("One or more validation errors occurred"));
        });
    }

    [Test]
    public void ToTypedResult_ConflictResult_ReturnsConflictWithProblem()
    {
        var result = TestResult.Conflict("Order already placed");

        var actual = result.ToTypedResult() as Conflict<ProblemDetails>;

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual!.StatusCode, Is.EqualTo(StatusCodes.Status409Conflict));
            Assert.That(actual.Value, Is.Not.Null);
            Assert.That(actual.Value!.Title, Is.EqualTo("A conflict occurred"));
        });
    }

    [Test]
    public void ToTypedResult_UnmappedErrorType_Throws()
    {
        var result = TestResult.Failure(new Error("Service down"), (ErrorType)999999);

        Assert.That(() => result.ToTypedResult(), Throws.TypeOf<UnreachableException>());
    }

    #endregion
}