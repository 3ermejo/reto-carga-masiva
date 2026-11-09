namespace Control.Api.Domain;

/// <summary>Fila de carga.carga_archivo (trazabilidad del archivo).</summary>
public class Carga
{
    public int Id { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public string? Periodos { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Resultado { get; set; }
    public string? Mensaje { get; set; }
    public int TotalRegistros { get; set; }
    public int RegistrosInsertados { get; set; }
    public int RegistrosFallidos { get; set; }
    public long TamanoBytes { get; set; }
    public string RutaArchivo { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public DateTime? FechaInicioProceso { get; set; }
    public DateTime? FechaFin { get; set; }
    public DateTime? FechaNotificacion { get; set; }
}
