namespace Throughline.Common.Events;

public interface IIntegrationEventBus
{
    Task PublishAsync(IntegrationEventBase @event, CancellationToken token = default);
}