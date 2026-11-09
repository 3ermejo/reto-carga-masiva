using Control.Api.Domain;

namespace Control.Api.Application.Interfaces;

public interface ICargaRepository
{
    Task<int> RegistrarAsync(string nombreArchivo, string usuario, int usuarioId, string rutaArchivo, long tamanoBytes, CancellationToken cancellationToken);
    Task FinalizarConErrorAsync(int idCarga, string mensaje, CancellationToken cancellationToken);
    Task<Carga?> ObtenerAsync(int idCarga, CancellationToken cancellationToken);
    Task<IReadOnlyList<CargaResumen>> ListarAsync(int? usuarioId, int limite, CancellationToken cancellationToken);
    Task<IReadOnlyList<CargaHistorial>> ListarHistorialAsync(int idCarga, CancellationToken cancellationToken);
    Task<IReadOnlyList<RegistroProcesado>> ListarRegistrosAsync(int idCarga, int pagina, int tamano, CancellationToken cancellationToken);
    Task<IReadOnlyList<RegistroFallido>> ListarFallidosAsync(int idCarga, int pagina, int tamano, CancellationToken cancellationToken);
}
