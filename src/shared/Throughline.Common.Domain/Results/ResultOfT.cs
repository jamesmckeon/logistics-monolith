namespace Throughline.Shared.Domain.Results;


public sealed record Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base() => Value = value;
    private Result(DomainError error) : base(error) { }

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(DomainError error) => new(error);
}