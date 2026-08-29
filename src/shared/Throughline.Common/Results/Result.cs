using System.Diagnostics.CodeAnalysis;

namespace Throughline.Common.Results;

public sealed class Result
{
    private Result()
    {
        Errors = Array.Empty<Error>();
        Succeeded = true;
    }

    private Result(Error[] errors, ErrorType errorType)
    {
        ErrorType = errorType;
        Errors = errors;
        Succeeded = false;
    }

    [MemberNotNullWhen(false, nameof(ErrorType))]
    public bool Succeeded { get; }

    public ErrorType? ErrorType { get; }
    public Error[] Errors { get; }

    public static Result Validation(IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var errorArray = errors.ToArray();

        if (!errorArray.Any())
            throw new ArgumentException("errors cannot be empty", nameof(errors));

        return new Result(errorArray, Results.ErrorType.Validation);
    }

    public static Result Success()
    {
        return new Result();
    }
}