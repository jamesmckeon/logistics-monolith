using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Throughline.Common.Results;

namespace Throughline.Common.Presentation;

public static class ResultProblemMapper
{
    public static ProblemDetails ToProblemDetails<T>(this Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Succeeded)
            throw new InvalidOperationException(
                "A ProblemDetails instance cannot be constructed from a successful result");

        var invalidParams = result.Errors.Where(e => e.FieldName != null)
            .Select(f => new { name = f.FieldName, reason = f.Description })
            .ToArray();

        var detail = result.Errors.Where(e => e.FieldName == null)
            .Select(e => e.Description)
            .ToArray();

        var problem = new ProblemDetails
        {
            Title = TitleFor(result.ErrorType.Value),
            Status = StatusFor(result.ErrorType.Value),
            Detail = detail.Length > 0 ? string.Join(" ", detail) : null
        };

        if (invalidParams.Length > 0)
            problem.Extensions["invalid-params"] = invalidParams;

        return problem;
    }

    private static int StatusFor(ErrorType type)
    {
        return type switch
        {
            ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => throw new UnreachableException($"No status mapping for ErrorType '{type}'.")
        };
    }

    private static string TitleFor(ErrorType type)
    {
        return type switch
        {
            ErrorType.Validation => "One or more validation errors occurred",
            ErrorType.Conflict => "The request conflicts with the current state of the resource",
            _ => throw new UnreachableException($"No title mapping for ErrorType '{type}'.")
        };
    }
}