namespace Throughline.Modules.Billing.Domain.Exceptions;

public class DomainValidationException : Exception
{
    public DomainValidationException() : base("An operation created invalid domain state")
    {
    }
}