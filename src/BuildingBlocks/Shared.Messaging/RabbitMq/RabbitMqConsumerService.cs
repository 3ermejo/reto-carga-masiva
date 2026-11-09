using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Shared.Messaging.RabbitMq;

/// <summary>
/// Servicio en segundo plano que escucha una cola y por cada mensaje
/// ejecuta el IMessageHandler correspondiente en su propio scope de DI.
/// - Ack manual: el mensaje solo se elimina de la cola cuando se proceso bien.
/// - Prefetch 1: cada instancia procesa un mensaje a la vez (para escalar se levantan mas replicas).
/// </summary>
public sealed class RabbitMqConsumerService<TMessage> : BackgroundService
{
    private const ushort PrefetchCount = 1;

    private readonly RabbitMqConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqConsumerService<TMessage>> _logger;
    private readonly string _queueName;
    private IChannel? _channel;

    public RabbitMqConsumerService(
        RabbitMqConnection connection,
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMqConsumerService<TMessage>> logger,
        string queueName)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _queueName = queueName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await StartConsumingAsync(stoppingToken);
                // La libreria se encarga de reconectar; aqui solo se espera hasta que se detenga el servicio
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo iniciar el consumidor de la cola {Queue}. Reintento en 5 segundos", _queueName);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task StartConsumingAsync(CancellationToken stoppingToken)
    {
        var connection = await _connection.GetConnectionAsync(stoppingToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        _channel = channel;

        await RabbitMqTopology.DeclareAsync(channel, stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: PrefetchCount, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, args) => OnMessageReceivedAsync(channel, args, stoppingToken);

        await channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        _logger.LogInformation("Escuchando la cola {Queue}", _queueName);
    }

    private async Task OnMessageReceivedAsync(IChannel channel, BasicDeliverEventArgs args, CancellationToken stoppingToken)
    {
        var messageId = args.BasicProperties.MessageId ?? "sin-id";

        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Cola"] = _queueName,
            ["MessageId"] = messageId,
            ["CorrelationId"] = args.BasicProperties.CorrelationId ?? string.Empty
        });

        TMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<TMessage>(args.Body.Span, MessageSerializer.Options);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Mensaje con formato invalido en {Queue}; se envia a la cola de error", _queueName);
            await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
            return;
        }

        if (message is null)
        {
            _logger.LogError("Mensaje vacio en {Queue}; se envia a la cola de error", _queueName);
            await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
            return;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<TMessage>>();
            await handler.HandleAsync(message, stoppingToken);

            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo procesar el mensaje {MessageId}; se envia a {Queue}.error", messageId, _queueName);
            await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            await _channel.CloseAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }
}
