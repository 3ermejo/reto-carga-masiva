using Notification.Worker.Application.Interfaces;
using Shared.Messaging.Contracts;
using Shared.Messaging.RabbitMq;

namespace Notification.Worker.Application.Services;

/// <summary>
/// Handler de la cola notificaciones: envia el correo al usuario y deja la carga en estado Notificado.
/// </summary>
public class CargaFinalizadaHandler : IMessageHandler<CargaFinalizadaMessage>
{
    private readonly ICargaRepository _repository;
    private readonly IEmailTemplateBuilder _templateBuilder;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<CargaFinalizadaHandler> _logger;

    public CargaFinalizadaHandler(
        ICargaRepository repository,
        IEmailTemplateBuilder templateBuilder,
        IEmailSender emailSender,
        ILogger<CargaFinalizadaHandler> logger)
    {
        _repository = repository;
        _templateBuilder = templateBuilder;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task HandleAsync(CargaFinalizadaMessage message, CancellationToken cancellationToken)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object> { ["IdCarga"] = message.IdCarga });

        var carga = await _repository.ObtenerAsync(message.IdCarga, cancellationToken);

        if (carga is null)
        {
            _logger.LogWarning("La carga no existe; no se envía la notificación");
            return;
        }

        // Idempotencia: si el mensaje llega dos veces no se envia el correo de nuevo
        if (carga.YaNotificada)
        {
            _logger.LogInformation("La carga ya fue notificada");
            return;
        }

        var email = _templateBuilder.Build(carga, message.Usuario);
        await _emailSender.SendAsync(email, cancellationToken);

        await _repository.MarcarNotificadoAsync(carga.Id, message.Usuario, cancellationToken);
        _logger.LogInformation("Notificación enviada; la carga pasó a estado Notificado");
    }
}
