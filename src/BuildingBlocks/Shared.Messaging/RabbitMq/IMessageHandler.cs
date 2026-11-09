namespace Shared.Messaging.RabbitMq;

/// <summary>
/// Logica que se ejecuta por cada mensaje. Si lanza una excepcion el mensaje
/// se rechaza y RabbitMQ lo envia a la cola .error (dead letter).
/// </summary>
public interface IMessageHandler<in TMessage>
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken);
}
