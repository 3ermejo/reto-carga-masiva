namespace Control.Api.Domain;

/// <summary>Cambio de estado registrado en carga.carga_archivo_historial (auditoria).</summary>
public class CargaHistorial
{
    public string Estado { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public string Origen { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}
