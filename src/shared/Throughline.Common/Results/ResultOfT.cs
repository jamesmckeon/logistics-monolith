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
        ArgumentNullException.ThrowIfNull(errorType);

        return new Result<T>([error], errorType);
    }

    public static Result<T> Failure(IEnumerable<Error> errors, ErrorType errorType)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var errorArray = errors.ToArray();

        if (!errorArray.Any())
            throw new ArgumentException("errors cannot be empty", nameof(errors));

        ArgumentNullException.ThrowIfNull(errorType);

        return new Result<T>(errorArray, errorType);
    }

    public static Result<T> Validation(params Error[] errors)
    {
        return Failure(errors, Results.ErrorType.Validation);
    }

    public static Result<T> Conflict(params Error[] errors)
    {
        return Failure(errors, Results.ErrorType.Conflict);
    }


    public static Result<T> Success(T value)
    {
        return new Result<T>(value);
    }
}