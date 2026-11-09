using RabbitMQ.Client;

namespace Shared.Messaging.RabbitMq;

/// <summary>
/// Topologia de RabbitMQ:
///   exchange "carga_masiva.exchange" (direct)
///     routing key "carga.registrada" -> cola "carga_masiva"
///     routing key "carga.finalizada" -> cola "notificaciones"
///   exchange "carga_masiva.dlx" (direct) -> colas "carga_masiva.error" y "notificaciones.error"
/// Los mensajes que fallan despues de los reintentos terminan en la cola .error
/// (dead letter) para poder revisarlos sin perderlos.
/// </summary>
public static class RabbitMqTopology
{
    public const string Exchange = "carga_masiva.exchange";
    public const string DeadLetterExchange = "carga_masiva.dlx";

    public const string CargaMasivaQueue = "carga_masiva";
    public const string NotificacionesQueue = "notificaciones";

    public const string CargaRegistradaRoutingKey = "carga.registrada";
    public const string CargaFinalizadaRoutingKey = "carga.finalizada";

    /// <summary>Declaracion idempotente: todos los servicios la ejecutan al iniciar.</summary>
    public static async Task DeclareAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: Exchange, type: ExchangeType.Direct, durable: true, autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: DeadLetterExchange, type: ExchangeType.Direct, durable: true, autoDelete: false,
            cancellationToken: cancellationToken);

        await DeclareQueueAsync(channel, CargaMasivaQueue, CargaRegistradaRoutingKey, cancellationToken);
        await DeclareQueueAsync(channel, NotificacionesQueue, CargaFinalizadaRoutingKey, cancellationToken);
    }

    private static async Task DeclareQueueAsync(IChannel channel, string queue, string routingKey, CancellationToken cancellationToken)
    {
        var errorQueue = $"{queue}.error";

        await channel.QueueDeclareAsync(
            queue: errorQueue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: errorQueue, exchange: DeadLetterExchange, routingKey: queue,
            cancellationToken: cancellationToken);

        var arguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = DeadLetterExchange,
            ["x-dead-letter-routing-key"] = queue
        };

        await channel.QueueDeclareAsync(
            queue: queue, durable: true, exclusive: false, autoDelete: false, arguments: arguments,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: queue, exchange: Exchange, routingKey: routingKey,
            cancellationToken: cancellationToken);
    }
}
