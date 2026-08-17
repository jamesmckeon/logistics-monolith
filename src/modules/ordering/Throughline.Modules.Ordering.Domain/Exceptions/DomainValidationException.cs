namespace Throughline.Modules.Ordering.Domain.Exceptions;

public class DomainValidationException : Exception
{
    public DomainValidationException() : base("An operation created invalid domain state")
    {
    }
}