using System.Diagnostics.CodeAnalysis;

namespace Throughline.Common.Results;

public sealed record Result<T>
{
    private Result(T value)
    {
        Value = value;
        Errors = Array.Empty<FieldError>();
        Succeeded = true;
    }

    private Result(FieldError[] errors, ErrorType errorType)
    {
        ErrorType = errorType;
        Errors = errors;
        Succeeded = false;
    }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(ErrorType))]
    public bool Succeeded { get; }

    public ErrorType? ErrorType { get; }
    public FieldError[] Errors { get; }
    public T? Value { get; }


    private static Result<T> Failure(IEnumerable<FieldError> errors, ErrorType errorType)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var errorArray = errors.ToArray();

        if (!errorArray.Any())
        {
            throw new ArgumentException("errors cannot be empty", nameof(errors));
        }

        return new Result<T>(errorArray, errorType);
    }


    public static Result<T> Validation(params FieldError[] errors) => Failure(errors, Results.ErrorType.Validation);

    public static Result<T> Validation(IEnumerable<FieldError> errors) => Failure(errors, Results.ErrorType.Validation);

    public static Result<T> Validation(params string[] errors)
    {
        return Failure(errors.Select(e => new FieldError(e)), Results.ErrorType.Validation);
    }

    public static Result<T> NotFound(string errorMessage) =>
        Failure([new FieldError(errorMessage)], Results.ErrorType.NotFound);

    public static Result<T> Conflict(params string[] errors)
    {
        // conflict result shouldn't carry errors with field names
        return Failure(errors.Select(e => new FieldError(e)),
            Results.ErrorType.Conflict);
    }


    public static Result<T> Success(T value) => new(value);

    public static implicit operator Result<T>(T value) => Success(value);

    public static Result<T> FromFailureResult(Result result)
    {
        if (result.Succeeded)
        {
            throw new ArgumentException("result.Succeeded must be false", nameof(result.Succeeded));
        }

        return Failure(result.Errors, result.ErrorType.Value);
    }
}