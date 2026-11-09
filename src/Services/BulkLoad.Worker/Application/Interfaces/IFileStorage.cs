namespace BulkLoad.Worker.Application.Interfaces;

public interface IFileStorage
{
    /// <summary>Descarga el archivo a memoria (ExcelDataReader necesita un stream con Seek).</summary>
    Task<Stream> DownloadAsync(string rutaArchivo, CancellationToken cancellationToken);
}
