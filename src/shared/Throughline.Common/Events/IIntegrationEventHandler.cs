namespace Throughline.Common.Events;

public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEventBase
{
    Task HandleAsync(TEvent @event, CancellationToken token = default);
}