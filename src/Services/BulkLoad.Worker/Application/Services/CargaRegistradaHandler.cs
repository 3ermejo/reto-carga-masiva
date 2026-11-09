using BulkLoad.Worker.Application.Interfaces;
using BulkLoad.Worker.Extensions;
using Polly.Registry;
using Shared.Messaging.Contracts;
using Shared.Messaging.RabbitMq;

namespace BulkLoad.Worker.Application.Services;

/// <summary>
/// Handler de la cola carga_masiva. Aplica el patron de reintentos (Polly) ante errores
/// temporales; si aun asi falla, la carga se marca con resultado Error, se notifica al
/// usuario y el mensaje se envia a la cola carga_masiva.error.
/// </summary>
public class CargaRegistradaHandler : IMessageHandler<CargaRegistradaMessage>
{
    private readonly ICargaProcessor _processor;
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;
    private readonly ILogger<CargaRegistradaHandler> _logger;

    public CargaRegistradaHandler(
        ICargaProcessor processor,
        ResiliencePipelineProvider<string> pipelineProvider,
        ILogger<CargaRegistradaHandler> logger)
    {
        _processor = processor;
        _pipelineProvider = pipelineProvider;
        _logger = logger;
    }

    public async Task HandleAsync(CargaRegistradaMessage message, CancellationToken cancellationToken)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object> { ["IdCarga"] = message.IdCarga });
        _logger.LogInformation("Mensaje recibido de la cola carga_masiva para el usuario {Usuario}", message.Usuario);

        var pipeline = _pipelineProvider.GetPipeline(ResilienceExtensions.ProcesamientoPipeline);

        try
        {
            await pipeline.ExecuteAsync(
                async token => await _processor.ProcesarAsync(message, token),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "La carga falló después de los reintentos");
            await _processor.FinalizarConErrorAsync(message, "Ocurrió un error técnico al procesar el archivo.", cancellationToken);
            throw;
        }
    }
}
