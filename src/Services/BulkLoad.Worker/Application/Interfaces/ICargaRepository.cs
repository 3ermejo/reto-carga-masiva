using BulkLoad.Worker.Domain;

namespace BulkLoad.Worker.Application.Interfaces;

public static class InicioProceso
{
    public const string Ok = "OK";
    public const string Omitir = "OMITIR";
    public const string NoExiste = "NO_EXISTE";

    // La carga ya estaba Finalizado pero su notificacion no se llego a publicar
    public const string ReenviarNotificacion = "REENVIAR_NOTIFICACION";
}

public interface ICargaRepository
{
    Task<string> IniciarProcesoAsync(int idCarga, CancellationToken cancellationToken);
    Task<ValidacionPeriodo> ValidarPeriodosAsync(int idCarga, IReadOnlyCollection<string> periodos, CancellationToken cancellationToken);
    Task<IReadOnlyList<ResultadoInsercion>> InsertarRegistrosAsync(int idCarga, IReadOnlyCollection<RegistroProducto> registros, CancellationToken cancellationToken);
    Task RegistrarFallidosAsync(int idCarga, IReadOnlyCollection<RegistroFallido> fallidos, CancellationToken cancellationToken);
    Task MarcarCargadoAsync(int idCarga, int registrosInsertados, CancellationToken cancellationToken);
    Task FinalizarAsync(int idCarga, int total, int insertados, int fallidos, string resultado, string? mensaje, CancellationToken cancellationToken);
}
