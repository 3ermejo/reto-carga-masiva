using Control.Api.Domain;

namespace Control.Api.Application.Dtos;

public record HistorialResponse(string Estado, string? Detalle, string Origen, DateTime Fecha);

// No se expone la ruta interna del archivo en SeaweedFS
public record CargaDetalleResponse(
    int Id,
    string NombreArchivo,
    string Usuario,
    string? Periodos,
    string Estado,
    string? Resultado,
    string? Mensaje,
    int TotalRegistros,
    int RegistrosInsertados,
    int RegistrosFallidos,
    long TamanoBytes,
    DateTime FechaRegistro,
    DateTime? FechaInicioProceso,
    DateTime? FechaFin,
    DateTime? FechaNotificacion,
    IReadOnlyList<HistorialResponse> Historial)
{
    public static CargaDetalleResponse From(Carga carga, IEnumerable<CargaHistorial> historial) => new(
        carga.Id,
        carga.NombreArchivo,
        carga.Usuario,
        carga.Periodos,
        carga.Estado,
        carga.Resultado,
        carga.Mensaje,
        carga.TotalRegistros,
        carga.RegistrosInsertados,
        carga.RegistrosFallidos,
        carga.TamanoBytes,
        carga.FechaRegistro,
        carga.FechaInicioProceso,
        carga.FechaFin,
        carga.FechaNotificacion,
        historial.Select(h => new HistorialResponse(h.Estado, h.Detalle, h.Origen, h.Fecha)).ToList());
}
