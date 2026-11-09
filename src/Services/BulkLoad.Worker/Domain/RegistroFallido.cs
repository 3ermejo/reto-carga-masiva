namespace BulkLoad.Worker.Domain;

/// <summary>Registro que no se cargo; se guarda en carga.detalle_carga (auditoria).</summary>
public record RegistroFallido(
    int Fila,
    string? CodigoProducto,
    string Motivo,
    string Detalle,
    IReadOnlyDictionary<string, string?> Datos);

public static class MotivoFallo
{
    public const string Existente = "Existente";
    public const string DatoInvalido = "DatoInvalido";
    public const string PeriodoRechazado = "PeriodoRechazado";
}
