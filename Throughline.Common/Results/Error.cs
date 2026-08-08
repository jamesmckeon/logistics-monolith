namespace Throughline.Common.Results;

public class Error
{
    protected Error(ErrorType errorType, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        ErorType = errorType;
        Description = description;
    }

    public ErrorType ErorType { get; }
    public string? Description { get; }

    public static Error Validation(string description)
    {
        return new Error(ErrorType.Validation, description);
    }
}