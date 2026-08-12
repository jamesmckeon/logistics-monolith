namespace Throughline.Common.Results;

public sealed record Result<T> : Result
{
    private Result(T value)
    {
        Value = value;
    }

    private Result(Error[] errors) : base(errors)
    {
    }

    public T? Value { get; }

    public static implicit operator Result<T>(T value)
    {
        return new Result<T>(value);
    }

    public static implicit operator Result<T>(Error[] errors)
    {
        return new Result<T>(errors);
    }

    public static new Result<T> Failed(Error error)
    {
        return new Error[] { error };
    }

    public static new Result<T> Failed(IEnumerable<Error> errors)
    {
        return errors.ToArray();
    }
}