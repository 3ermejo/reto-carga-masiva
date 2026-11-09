namespace Shared.Messaging.Contracts;

/// <summary>Resultado con el que termina una carga (columna carga_archivo.resultado).</summary>
public static class ResultadoCarga
{
    public const string Exitoso = "Exitoso";
    public const string ConObservaciones = "ConObservaciones";
    public const string Rechazado = "Rechazado";
    public const string Error = "Error";
}
