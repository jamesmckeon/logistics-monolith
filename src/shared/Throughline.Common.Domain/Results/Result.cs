namespace Throughline.Shared.Domain.Results;

public record Result
{
    public bool Success { get; }
    public DomainError? Error { get; }

    protected Result(DomainError error)
    {

        ArgumentNullException.ThrowIfNull(error);

        Error = error;
        Success = false;
    }

    protected Result()
    {
        Success = true;
    }

    public static Result Succeeded() => new();
    public static Result Failed(DomainError error) => new(error);
}
