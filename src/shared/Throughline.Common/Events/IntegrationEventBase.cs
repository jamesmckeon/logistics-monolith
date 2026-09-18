namespace Throughline.Common.Events;

public abstract record IntegrationEventBase
{
    public Guid Id { get; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}