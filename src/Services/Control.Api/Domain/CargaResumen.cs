namespace Control.Api.Domain;

/// <summary>Fila del historial de cargas (carga.fn_listar_cargas).</summary>
public class CargaResumen
{
    public int Id { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string? Periodos { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Resultado { get; set; }
    public int TotalRegistros { get; set; }
    public int RegistrosInsertados { get; set; }
    public int RegistrosFallidos { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime? FechaFin { get; set; }
}
