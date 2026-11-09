namespace BulkLoad.Worker.Application.Services;

public class ProcesamientoOptions
{
    public const string SectionName = "Procesamiento";

    /// <summary>Limite de filas por archivo (protege contra archivos excesivos).</summary>
    public int MaxFilas { get; set; } = 100_000;

    public int MaxArchivoMB { get; set; } = 20;
}
