namespace Control.Api.Application.Interfaces;

public interface IFileStorage
{
    /// <summary>Guarda el archivo y devuelve su ruta logica (seaweed://...).</summary>
    Task<string> UploadAsync(string relativePath, Stream content, string contentType, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken);
}
