namespace Throughline.Common.Results;

public record Result
{
    protected Result(IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (!errors.Any())
            throw new ArgumentException("errors must contain at least one error", nameof(errors));

        Errors = errors;
        Success = false;
    }

    protected Result()
    {
        Success = true;
        Errors = Enumerable.Empty<Error>();
    }

    public bool Success { get; }
    public IEnumerable<Error> Errors { get; }

    public static Result Succeeded()
    {
        return new Result();
    }

    public static Result Failed(IEnumerable<Error> errors)
    {
        return new Result(errors);
    }
}