using Throughline.Common.Results;

namespace Throughline.Common.Presentation.Tests;

using TestResult = Result<object>;

[Category("Unit")]
public sealed class ResultProblemMapperTests
{
    [Test]
    public void ToProblemDetails_SuccessResult_ThrowsExpected()
    {
        var result = TestResult.Success(new());
        var ex = Assert.Throws<InvalidOperationException>(() => result.ToProblemDetails());

        Assert.That(ex.Message,
            Is.EqualTo("A ProblemDetails instance cannot be constructed from a successful result"));
    }

    [Test]
    public void ToProblemDetails_ValidationWithFieldErrors_ReturnsInvalidParams()
    {
        var errors = new Error[]
        {
            new("Test Error 1", "Field 1"),
            new("Test Error 2", "Field 2")
        };

        var result = TestResult.Validation(errors);
        var actual = result.ToProblemDetails();

        Assert.Multiple(() =>
        {
            Assert.That(actual.Title, Is.EqualTo("One or more validation errors occurred"));
            Assert.That(actual.Detail, Is.Null);
            Assert.That(actual.Extensions.Count, Is.EqualTo(2));
            Assert.That(
                actual.Extensions.Any(s => s.Key == errors.First().FieldName && s.Value == errors.First().Description),
                Is.True);
            Assert.That(
                actual.Extensions.Any(s => s.Key == errors.Last().FieldName && s.Value == errors.Last().Description),
                Is.True);
        });

        Assert.That(ex.Message,
            Is.EqualTo("A ProblemDetails instance cannot be constructed from a successful result"));
    }
}