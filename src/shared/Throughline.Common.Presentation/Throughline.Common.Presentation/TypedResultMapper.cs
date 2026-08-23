using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Throughline.Common.Results;

namespace Throughline.Common.Presentation;

public static class TypedResultMapper
{
    public static IResult ToTypedResult<T>(this Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Succeeded)
            return TypedResults.Ok(result.Value);

        return result.ErrorType switch
        {
            ErrorType.Validation => TypedResults.BadRequest(result.ToProblemDetails()),
            ErrorType.Conflict => TypedResults.Conflict(result.ToProblemDetails()),
            _ => throw new UnreachableException($"No status mapping for ErrorType '{result.ErrorType}'.")
        };
    }
}