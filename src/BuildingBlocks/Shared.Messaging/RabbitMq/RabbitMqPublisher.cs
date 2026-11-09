using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Shared.Messaging.RabbitMq;

/// <summary>
/// Publica en el exchange direct con confirmacion del broker (publisher confirms):
/// si RabbitMQ no confirma que guardo el mensaje, se lanza una excepcion.
/// Los mensajes son persistentes (sobreviven a un reinicio de RabbitMQ).
/// </summary>
public sealed class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly RabbitMqConnection _connection;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IChannel? _channel;

    public RabbitMqPublisher(RabbitMqConnection connection, ILogger<RabbitMqPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task PublishAsync<TMessage>(TMessage message, string routingKey, string correlationId, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message, MessageSerializer.Options);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = correlationId,
            Type = typeof(TMessage).Name,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        // El canal no se debe usar desde varios hilos a la vez
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var channel = await GetChannelAsync(cancellationToken);
            await channel.BasicPublishAsync(
                exchange: RabbitMqTopology.Exchange,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }
        finally
        {
            _lock.Release();
        }

        _logger.LogInformation(
            "Mensaje {MessageType} publicado con routing key {RoutingKey} (correlacion {CorrelationId})",
            typeof(TMessage).Name, routingKey, correlationId);
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        var connection = await _connection.GetConnectionAsync(cancellationToken);
        var options = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);

        _channel = await connection.CreateChannelAsync(options, cancellationToken);
        await RabbitMqTopology.DeclareAsync(_channel, cancellationToken);
        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }

        _lock.Dispose();
    }
}
