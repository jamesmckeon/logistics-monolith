namespace Throughline.Shared.Domain.Results;

public class DomainError
{
    public ErrorTypes ErorType { get; }
    public string? Description  { get; }
    
    protected  DomainError(ErrorTypes errorType,  string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        
        ErorType = errorType;
        Description = description;
    }
    
    public static DomainError Validation(string description) => new(ErrorTypes.Validation, description);
}