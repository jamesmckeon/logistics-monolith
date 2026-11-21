namespace Throughline.Common.Results;

public sealed record FieldError
{
    public FieldError(string description, string? fieldName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Description = description;
        FieldName = fieldName;
    }

    public string Description { get; }
    public string? FieldName { get; }

    public static FieldError IsRequired(string paramName) =>
        new(
            $"{paramName} is required.",
            paramName
        );

    public static FieldError LessThanOne(string paramName) =>
        new(
            $"{paramName} must be greater than zero.",
            paramName
        );
}