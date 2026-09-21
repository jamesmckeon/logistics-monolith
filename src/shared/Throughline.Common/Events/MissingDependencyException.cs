namespace Throughline.Common.Events;

/// <summary>
///     Thrown by a message handler when the message references data that is expected to exist
///     but isn't present locally yet
///     Drives retry + backoff in Wolverine
/// </summary>
public sealed class MissingDependencyException : Exception
{
    public MissingDependencyException(string message) : base(message)
    {
    }

    public MissingDependencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}