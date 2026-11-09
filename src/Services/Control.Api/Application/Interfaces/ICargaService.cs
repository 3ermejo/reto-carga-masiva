using Control.Api.Application.Common;
using Control.Api.Application.Dtos;
using Control.Api.Domain;

namespace Control.Api.Application.Interfaces;

public interface ICargaService
{
    Task<OperationResult<CargaAceptadaResponse>> RegistrarAsync(IFormFile? archivo, CurrentUser usuario, CancellationToken cancellationToken);
    Task<IReadOnlyList<CargaResumen>> ListarAsync(CurrentUser usuario, CancellationToken cancellationToken);
    Task<CargaDetalleResponse?> ObtenerDetalleAsync(int idCarga, CurrentUser usuario, CancellationToken cancellationToken);
    Task<PagedResult<RegistroProcesado>?> ListarRegistrosAsync(int idCarga, int pagina, int tamano, CurrentUser usuario, CancellationToken cancellationToken);
    Task<PagedResult<RegistroFallido>?> ListarFallidosAsync(int idCarga, int pagina, int tamano, CurrentUser usuario, CancellationToken cancellationToken);
    Task<ArchivoDescarga?> DescargarArchivoAsync(int idCarga, CurrentUser usuario, CancellationToken cancellationToken);
}
