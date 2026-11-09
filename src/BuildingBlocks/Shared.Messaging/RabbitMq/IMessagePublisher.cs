namespace Shared.Messaging.RabbitMq;

public interface IMessagePublisher
{
    Task PublishAsync<TMessage>(TMessage message, string routingKey, string correlationId, CancellationToken cancellationToken);
}
