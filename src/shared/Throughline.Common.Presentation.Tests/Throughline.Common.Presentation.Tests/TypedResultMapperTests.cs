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


    [Test]
    public void Created_SuccessResult_ReturnsCreatedAt()
    {
        var result = TestResult.Success(SuccessValue);

        var route = "/test/1";
        var actual = (Created<object>)result.Created(route);

        Assert.Multiple(() =>
        {
            Assert.That(actual.StatusCode, Is.EqualTo(StatusCodes.Status201Created));
            Assert.That(actual.Location, Is.SameAs(route));
            Assert.That(actual.Value, Is.SameAs(result.Value));
        });
    }

    [Test]
    public void Created_ValidationResult_ReturnsBadRequestWithProblem()
    {
        var result = TestResult.Validation("Bad input");
        var actual = result.Created("test") as BadRequest<ProblemDetails>;

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(actual.Value, Is.Not.Null);
            Assert.That(actual.Value!.Title, Is.EqualTo("One or more validation errors occurred"));
        });
    }

    [Test]
    public void Created_ConflictResult_ReturnsConflictWithProblem()
    {
        var result = TestResult.Conflict("Order already placed");
        var actual = result.Created("test") as Conflict<ProblemDetails>;

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual!.StatusCode, Is.EqualTo(StatusCodes.Status409Conflict));
            Assert.That(actual.Value, Is.Not.Null);
            Assert.That(actual.Value!.Title, Is.EqualTo("A conflict occurred"));
        });
    }
}