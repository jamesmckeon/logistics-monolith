using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Throughline.Common.Results;

namespace Throughline.Common.Presentation;

public static class ResultProblemMapper
{
    /// <summary>
    ///     Maps a <b>failed</b> <see cref="Result{T}" /> to an RFC 7807 <see cref="ProblemDetails" />.
    /// </summary>
    /// <typeparam name="T">The success value type of the result (unused on the failure path).</typeparam>
    /// <param name="result">The failed result to map. Must not be null.</param>
    /// <returns>
    ///     A <see cref="ProblemDetails" /> whose <c>Title</c>, <c>Status</c>, optional <c>Detail</c>, and
    ///     optional <c>invalid-params</c> extension are populated as described above.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="result" /> is null.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="result" /> is a success.</exception>
    /// <exception cref="UnreachableException">
    ///     The result's <see cref="ErrorType" /> has no Title/Status mapping.
    /// </exception>
    public static ProblemDetails ToProblemDetails<T>(this Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Succeeded)
            throw new InvalidOperationException(
                "A ProblemDetails instance cannot be constructed from a successful result");


        var title = TitleFor(result.ErrorType.Value);
        var status = StatusFor(result.ErrorType.Value);

        if (result.Errors.All(a => a.FieldName != null))
            return new ValidationProblemDetails(
                result.Errors
                    .GroupBy(e => e.FieldName!)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()))
            {
                Title = title,
                Status = status
            };

        var detail = DetailFor(result.Errors);

        var problem = new ProblemDetails
        {
            Title = TitleFor(result.ErrorType.Value),
            Status = StatusFor(result.ErrorType.Value),
            Detail = detail
        };

        return problem;
    }

    private static int StatusFor(ErrorType type)
    {
        return type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => throw new UnreachableException($"No status mapping for ErrorType '{type}'.")
        };
    }

    private static string TitleFor(ErrorType type)
    {
        return type switch
        {
            ErrorType.Validation => "One or more validation errors occurred",
            ErrorType.Conflict => "A conflict occurred",
            _ => throw new UnreachableException($"No title mapping for ErrorType '{type}'.")
        };
    }


    private static string DetailFor(Error[] errors)
    {
        var fieldErrors = errors.Where(e => e.FieldName != null)
            .Select(f => new { name = f.FieldName, reason = f.Description })
            .ToArray();

        var nonFieldErrors = errors.Where(e => e.FieldName == null)
            .Select(e => e.Description)
            .ToArray();


        if (!fieldErrors.Any())
            return string.Join("; ", nonFieldErrors);

        var all = new[]
        {
            "One or more fields are invalid"
        }.Concat(nonFieldErrors);

        return string.Join("; ", all);
    }
}