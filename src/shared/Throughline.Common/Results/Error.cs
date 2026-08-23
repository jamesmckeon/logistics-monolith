namespace Throughline.Common.Results;

public sealed record Error
{
    public Error(string description, string? fieldName = null)
    {
        ArgumentNullException.ThrowIfNull(description);

        Description = description;
        FieldName = fieldName;
    }

    public string Description { get; }
    public string? FieldName { get; }
}