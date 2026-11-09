namespace Notification.Worker.Domain;

/// <summary>Datos de la carga que se usan para armar el correo (carga.fn_obtener_carga).</summary>
public class CargaNotificacion
{
    public int Id { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string? Periodos { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Resultado { get; set; }
    public string? Mensaje { get; set; }
    public int TotalRegistros { get; set; }
    public int RegistrosInsertados { get; set; }
    public int RegistrosFallidos { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime? FechaFin { get; set; }

    public bool YaNotificada => Estado == "Notificado";
}
