namespace Control.Api.Application.Common;

public class UploadOptions
{
    public const string SectionName = "Upload";

    /// <summary>Tamaño maximo del archivo (configurable por variable de entorno Upload__MaxFileSizeMB).</summary>
    public int MaxFileSizeMB { get; set; } = 10;

    public long MaxFileSizeBytes => MaxFileSizeMB * 1024L * 1024L;

    // Margen para los headers del multipart/form-data
    public long MaxRequestBodyBytes => MaxFileSizeBytes + 1024L * 1024L;
}
