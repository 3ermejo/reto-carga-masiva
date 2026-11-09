namespace BulkLoad.Worker.Domain;

/// <summary>Respuesta de carga.fn_insertar_registros por cada fila enviada.</summary>
public class ResultadoInsercion
{
    public int Fila { get; set; }
    public string CodigoProducto { get; set; } = string.Empty;
    public bool Insertado { get; set; }
}
