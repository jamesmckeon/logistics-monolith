namespace Throughline.Common.Events;

public abstract record IntegrationEventBase(Guid Id, DateTimeOffset OccurredOnUtc)
{
}