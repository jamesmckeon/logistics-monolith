namespace Throughline.Common.Results;

public sealed record Result<T> : Result
{
    private Result(T value) : base()
    {
        Value = value;
    }

    private Result(Error error) : base(error)
    {
    }

    public T? Value { get; }

    public static implicit operator Result<T>(T value)
    {
        return new Result<T>(value);
    }

    public static implicit operator Result<T>(Error error)
    {
        return new Result<T>(error);
    }
}