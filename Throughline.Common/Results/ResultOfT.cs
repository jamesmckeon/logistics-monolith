namespace Throughline.Common.Results;

public sealed record Result<T>
{
    private Result(T value)
    {
        Value = value;
        Errors = Array.Empty<Error>();
        Succeeded = true;
    }

    private Result(Error[] errors)
    {
        Errors = errors;
        Succeeded = false;
    }

    public bool Succeeded { get; }
    public IEnumerable<Error> Errors { get; }
    public T? Value { get; }


    public static implicit operator Result<T>(T value)
    {
        return new Result<T>(value);
    }

    public static implicit operator Result<T>(Error[] errors)
    {
        return new Result<T>(errors);
    }

    public static Result<T> Failure(Error error)
    {
        return new[] { error };
    }

    public static Result<T> Failure(IEnumerable<Error> errors)
    {
        return errors.ToArray();
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(value);
    }
}