using System.Net;
using BulkLoad.Worker.Application.Exceptions;
using BulkLoad.Worker.Application.Interfaces;
using BulkLoad.Worker.Application.Services;
using Microsoft.Extensions.Options;

namespace BulkLoad.Worker.Infrastructure.Storage;

/// <summary>
/// Descarga archivos del Filer de SeaweedFS. Las rutas vienen como "seaweed://uploads/...".
/// El HttpClient tiene reintentos y circuit breaker (ver AddFileStorage).
/// </summary>
public class SeaweedFsStorage : IFileStorage
{
    private const string Scheme = "seaweed://";
    private const string AllowedRoot = "uploads/";

    private readonly HttpClient _httpClient;
    private readonly long _maxBytes;

    public SeaweedFsStorage(HttpClient httpClient, IOptions<ProcesamientoOptions> options)
    {
        _httpClient = httpClient;
        _maxBytes = options.Value.MaxArchivoMB * 1024L * 1024L;
    }

    public async Task<Stream> DownloadAsync(string rutaArchivo, CancellationToken cancellationToken)
    {
        var relativePath = ToRelativePath(rutaArchivo);

        using var response = await _httpClient.GetAsync(relativePath, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new FileNotFoundException("El archivo ya no existe en el almacenamiento.");

        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength > _maxBytes)
            throw new InvalidExcelException("El archivo supera el tamaño máximo permitido para procesar.");

        var buffer = new MemoryStream();
        await response.Content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }

    // Defensa en profundidad: solo se leen archivos de la carpeta de cargas (evita SSRF / path traversal)
    private static string ToRelativePath(string rutaArchivo)
    {
        if (!rutaArchivo.StartsWith(Scheme, StringComparison.Ordinal))
            throw new FileNotFoundException("La ruta del archivo no es válida.");

        var relativePath = rutaArchivo[Scheme.Length..];
        if (!relativePath.StartsWith(AllowedRoot, StringComparison.Ordinal) || relativePath.Contains(".."))
            throw new FileNotFoundException("La ruta del archivo no es válida.");

        return relativePath;
    }
}
