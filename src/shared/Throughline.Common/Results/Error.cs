namespace Throughline.Common.Results;

public sealed record Error
{
    public Error(string description, string? fieldName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Description = description;
        FieldName = fieldName;
    }

    public string Description { get; }
    public string? FieldName { get; }

    public static Error IsRequired(string paramName)
    {
        return new Error(
            $"{paramName} is required.",
            paramName
        );
    }
}