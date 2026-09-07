using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Throughline.Common.Results;

namespace Throughline.Common.Presentation;

public static class FailedResultResponseMapper
{
    public static IResult ToFailureResponse<T>(this Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Succeeded)
            throw new InvalidOperationException("Cannot map a successful result to a failure response");

        return result.ErrorType switch
        {
            ErrorType.Validation => TypedResults.BadRequest(result.ToProblemDetails()),
            ErrorType.Conflict => TypedResults.Conflict(result.ToProblemDetails()),
            _ => throw new UnreachableException($"No status mapping for ErrorType '{result.ErrorType}'.")
        };
    }
}