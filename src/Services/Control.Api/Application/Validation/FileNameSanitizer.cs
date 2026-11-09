using System.Text.RegularExpressions;

namespace Control.Api.Application.Validation;

/// <summary>
/// El nombre original solo se guarda para mostrarlo; en SeaweedFS el archivo
/// se guarda con un nombre generado por el servidor.
/// </summary>
public static class FileNameSanitizer
{
    private const int MaxLength = 150;

    private static readonly Regex InvalidCharacters = new(
        @"[^\w\-. ()]",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    public static string Sanitize(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "archivo.xlsx";

        // Algunos navegadores envian la ruta completa (C:\...\archivo.xlsx)
        var name = Path.GetFileName(fileName.Replace('\\', '/'));
        name = InvalidCharacters.Replace(name, "_").Trim();

        if (name.Length > MaxLength)
            name = name[^MaxLength..];

        return string.IsNullOrWhiteSpace(name) ? "archivo.xlsx" : name;
    }
}
