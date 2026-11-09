namespace BulkLoad.Worker.Domain;

/// <summary>Respuesta de carga.fn_validar_periodos.</summary>
public class ValidacionPeriodo
{
    public const string Ok = "OK";
    public const string Rechazado = "RECHAZADO";
    public const string Bloqueado = "BLOQUEADO";

    public string Resultado { get; set; } = string.Empty;
    public string? Periodo { get; set; }
    public int? CargaConflicto { get; set; }
    public string? EstadoConflicto { get; set; }

    public bool EsValido => Resultado == Ok;
}
