using Notification.Worker.Domain;

namespace Notification.Worker.Application.Interfaces;

public interface ICargaRepository
{
    Task<CargaNotificacion?> ObtenerAsync(int idCarga, CancellationToken cancellationToken);
    Task MarcarNotificadoAsync(int idCarga, string destinatario, CancellationToken cancellationToken);
}
