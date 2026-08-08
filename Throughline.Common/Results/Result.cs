namespace Throughline.Common.Results;

public record Result
{
    protected Result(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        Error = error;
        Success = false;
    }

    protected Result()
    {
        Success = true;
    }

    public bool Success { get; }
    public Error? Error { get; }

    public static Result Succeeded()
    {
        return new Result();
    }

    public static Result Failed(Error error)
    {
        return new Result(error);
    }
}