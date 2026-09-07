using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Throughline.Common.Results;

namespace Throughline.Common.Presentation.Tests;

[Category("Unit")]
public sealed class FailedResultResponseMapperTests
{
    [Test]
    public void ToFailureResponse_SuccessfulResult_ThrowsExpected()
    {
        var result = Result<object>.Success(new object());
        var ex = Assert.Throws<InvalidOperationException>(() => result.ToFailureResponse());
        Assert.That(ex.Message, Is.EqualTo("Cannot map a successful result to a failure response"));
    }

    [Test]
    public void ToFailureResponse_ValidationError_ReturnsBadRequest()
    {
        var result = Result<object>.Validation(new Error("Test description", "Test Field"));
        var actual = (BadRequest<ProblemDetails>)result.ToFailureResponse();
        // don't need to verify ProblemDetails equality, that's handled by ResultProblemMapperTests
        Assert.That(actual.Value, Is.InstanceOf<ProblemDetails>());
    }

    [Test]
    public void ToFailureResponse_ConflictError_ReturnsConflict()
    {
        var result = Result<object>.Conflict("Test Error");
        var actual = (Conflict<ProblemDetails>)result.ToFailureResponse();
        // don't need to verify ProblemDetails equality, that's handled by ResultProblemMapperTests
        Assert.That(actual.Value, Is.InstanceOf<ProblemDetails>());
    }

    private static bool AreEqual(ProblemDetails x, ProblemDetails y)
    {
        return x.Type == y.Type && x.Title == y.Title && x.Status == y.Status && x.Detail == y.Detail &&
               x.Instance == y.Instance && x.Extensions.Equals(y.Extensions);
    }
}