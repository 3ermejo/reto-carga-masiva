using System.IO.Compression;
using Control.Api.Application.Common;
using Control.Api.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Control.Api.Application.Validation;

/// <summary>
/// Validaciones del archivo antes de guardarlo (OWASP File Upload):
/// extension permitida, tamaño maximo configurado y que el contenido sea realmente un .xlsx.
/// No se confia en el Content-Type que envia el navegador porque lo controla el cliente.
/// </summary>
public class ExcelFileValidator : IExcelFileValidator
{
    public const string AllowedExtension = ".xlsx";

    // Un .xlsx es un ZIP: empieza con "PK\x03\x04"
    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];

    private readonly UploadOptions _options;

    public ExcelFileValidator(IOptions<UploadOptions> options)
    {
        _options = options.Value;
    }

    public FileValidationResult Validate(string fileName, long length, Stream content)
    {
        if (length <= 0)
            return FileValidationResult.Invalid("El archivo está vacío.");

        if (length > _options.MaxFileSizeBytes)
            return FileValidationResult.Invalid($"El archivo supera el tamaño máximo permitido de {_options.MaxFileSizeMB} MB.");

        if (!string.Equals(Path.GetExtension(fileName), AllowedExtension, StringComparison.OrdinalIgnoreCase))
            return FileValidationResult.Invalid("Solo se permiten archivos Excel con extensión .xlsx.");

        if (!HasZipSignature(content) || !ContainsWorkbook(content))
            return FileValidationResult.Invalid("El contenido del archivo no corresponde a un Excel (.xlsx) válido.");

        return FileValidationResult.Valid();
    }

    private static bool HasZipSignature(Stream content)
    {
        var header = new byte[ZipSignature.Length];
        var read = content.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
        content.Position = 0;
        return read == header.Length && header.AsSpan().SequenceEqual(ZipSignature);
    }

    private static bool ContainsWorkbook(Stream content)
    {
        try
        {
            using var zip = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
            return zip.GetEntry("xl/workbook.xml") is not null;
        }
        catch (InvalidDataException)
        {
            return false;
        }
        finally
        {
            content.Position = 0;
        }
    }
}
