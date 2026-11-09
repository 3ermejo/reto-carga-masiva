using Shared.Messaging.Contracts;

namespace BulkLoad.Worker.Application.Interfaces;

public interface ICargaProcessor
{
    Task ProcesarAsync(CargaRegistradaMessage mensaje, CancellationToken cancellationToken);
    Task FinalizarConErrorAsync(CargaRegistradaMessage mensaje, string motivo, CancellationToken cancellationToken);
}
