namespace Control.Api.Domain;

/// <summary>Fila de carga.data_procesada: registro extraido del Excel.</summary>
public class RegistroProcesado
{
    public int Fila { get; set; }
    public string Periodo { get; set; } = string.Empty;
    public string CodigoProducto { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public long TotalFilas { get; set; }
}
