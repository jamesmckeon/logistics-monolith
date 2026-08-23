using System.Diagnostics.CodeAnalysis;

namespace Throughline.Common.Results;

public sealed record Result<T>
{
    private Result(T value)
    {
        Value = value;
        Errors = Array.Empty<Error>();
        Succeeded = true;
    }

    private Result(Error[] errors, ErrorType errorType)
    {
        ErrorType = errorType;
        Errors = errors;
        Succeeded = false;
    }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(ErrorType))]
    public bool Succeeded { get; }

    public ErrorType? ErrorType { get; }
    public Error[] Errors { get; }
    public T? Value { get; }


    public static Result<T> Failure(Error error, ErrorType errorType)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result<T>([error], errorType);
    }

    public static Result<T> Failure(IEnumerable<Error> errors, ErrorType errorType)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var errorArray = errors.ToArray();

        if (!errorArray.Any())
            throw new ArgumentException("errors cannot be empty", nameof(errors));

        return new Result<T>(errorArray, errorType);
    }

    public static Result<T> Validation(IEnumerable<Error> errors)
    {
        return Failure(errors, Results.ErrorType.Validation);
    }

    public static Result<T> Validation(IEnumerable<string> errors)
    {
        return Failure(errors.Select(e => new Error(e)), Results.ErrorType.Validation);
    }

    public static Result<T> Conflict(params string[] errors)
    {
        // conflict result shouldn't carry errors with field names
        return Failure(errors.Select(e => new Error(e)),
            Results.ErrorType.Conflict);
    }


    public static Result<T> Success(T value)
    {
        return new Result<T>(value);
    }

    public static implicit operator Result<T>(T value)
    {
        return Success(value);
    }
}