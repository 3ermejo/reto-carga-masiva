using System.Net;
using System.Net.Http.Headers;
using Control.Api.Application.Interfaces;

namespace Control.Api.Infrastructure.Storage;

/// <summary>
/// Almacenamiento en SeaweedFS usando la API HTTP del Filer:
/// POST multipart a una ruta crea el archivo; GET a la ruta lo descarga.
/// Las rutas se guardan en la BD como "seaweed://uploads/2026/11/xxxx.xlsx".
/// El HttpClient tiene reintentos y circuit breaker (ver AddFileStorage).
/// </summary>
public class SeaweedFsStorage : IFileStorage
{
    public const string Scheme = "seaweed://";
    private const string AllowedRoot = "uploads/";

    private readonly HttpClient _httpClient;

    public SeaweedFsStorage(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> UploadAsync(string relativePath, Stream content, string contentType, CancellationToken cancellationToken)
    {
        EnsureAllowed(relativePath);

        // Se copia a memoria para que los reintentos puedan reenviar el archivo completo
        content.Position = 0;
        var bytes = new byte[content.Length];
        await content.ReadExactlyAsync(bytes, cancellationToken);

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "file", Path.GetFileName(relativePath));

        using var response = await _httpClient.PostAsync(relativePath, form, cancellationToken);
        response.EnsureSuccessStatusCode();

        return Scheme + relativePath;
    }

    public async Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
    {
        var relativePath = ToRelativePath(storagePath);

        var response = await _httpClient.GetAsync(relativePath, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            response.Dispose();
            throw new FileNotFoundException("El archivo no existe en el almacenamiento.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            response.Dispose();
            throw new HttpRequestException($"SeaweedFS respondió {statusCode}.");
        }

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    private static string ToRelativePath(string storagePath)
    {
        if (!storagePath.StartsWith(Scheme, StringComparison.Ordinal))
            throw new ArgumentException("Ruta de almacenamiento no válida.", nameof(storagePath));

        var relativePath = storagePath[Scheme.Length..];
        EnsureAllowed(relativePath);
        return relativePath;
    }

    // Defensa en profundidad: solo se trabaja dentro de la carpeta de cargas
    private static void EnsureAllowed(string relativePath)
    {
        if (!relativePath.StartsWith(AllowedRoot, StringComparison.Ordinal) || relativePath.Contains(".."))
            throw new ArgumentException("Ruta de archivo no permitida.", nameof(relativePath));
    }
}
