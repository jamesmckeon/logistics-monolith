namespace Throughline.Modules.Ordering.Domain.Common.Exceptions;

public class EmptyCollectionException : Exception
{
    public EmptyCollectionException(string message) : base(message)
    {
    }

    public static void ThrowIfNullOrEmpty<T>(IEnumerable<T>? argument, string paramName)
    {
        if (argument is null || !argument.Any())
            throw new EmptyCollectionException($"Parameter '{paramName}' must contain at least one item");
    }
}