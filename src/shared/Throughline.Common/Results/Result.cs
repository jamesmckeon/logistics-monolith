using System.Diagnostics.CodeAnalysis;

namespace Throughline.Common.Results;

public sealed class Result
{
    private Result()
    {
        Errors = Array.Empty<FieldError>();
        Succeeded = true;
    }

    private Result(FieldError[] errors, ErrorType errorType)
    {
        ErrorType = errorType;
        Errors = errors;
        Succeeded = false;
    }

    [MemberNotNullWhen(false, nameof(ErrorType))]
    public bool Succeeded { get; }

    public ErrorType? ErrorType { get; }
    public FieldError[] Errors { get; }

    public static Result Validation(IEnumerable<FieldError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var errorArray = errors.ToArray();

        if (!errorArray.Any())
            throw new ArgumentException("errors cannot be empty", nameof(errors));

        return new Result(errorArray, Results.ErrorType.Validation);
    }

    public static Result Validation(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return Validation([new(errorMessage)]);
    }

    public static Result Conflict(IEnumerable<FieldError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var errorArray = errors.ToArray();

        if (!errorArray.Any())
            throw new ArgumentException("errors cannot be empty", nameof(errors));

        return new Result(errorArray, Results.ErrorType.Conflict);
    }

    public static Result Conflict(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return Conflict([new FieldError(errorMessage)]);
    }

    public static Result Success()
    {
        return new Result();
    }
}