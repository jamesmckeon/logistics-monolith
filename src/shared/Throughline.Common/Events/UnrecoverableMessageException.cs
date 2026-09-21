namespace Throughline.Common.Events;

/// <summary>
///     Thrown by a message handler when a message can never be processed successfully — a
///     permanent (poison) failure such as invalid/contract-violating data.
/// </summary>
public sealed class UnrecoverableMessageException : Exception
{
    public UnrecoverableMessageException(string message) : base(message)
    {
    }

    public UnrecoverableMessageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}