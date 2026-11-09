namespace Control.Api.Domain;

/// <summary>Fila de carga.detalle_carga: registro que no se cargo y por que.</summary>
public class RegistroFallido
{
    public int Fila { get; set; }
    public string? CodigoProducto { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public long TotalFilas { get; set; }
}
