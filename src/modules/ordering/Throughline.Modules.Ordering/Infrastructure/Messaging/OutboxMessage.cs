namespace Throughline.Modules.Ordering.Infrastructure.Messaging;

internal sealed class OutboxMessage
{
    public Guid Id { get; init; }
    public DateTimeOffset OccurredOnUtc { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset? ProcessedOnUtc { get; set; }
    public int Attempts { get; set; }
    public string? Error { get; set; }
}