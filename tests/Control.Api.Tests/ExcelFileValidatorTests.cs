using System.IO.Compression;
using System.Text;
using Control.Api.Application.Common;
using Control.Api.Application.Validation;
using Microsoft.Extensions.Options;

namespace Control.Api.Tests;

public class ExcelFileValidatorTests
{
    private readonly ExcelFileValidator _validator = new(Options.Create(new UploadOptions { MaxFileSizeMB = 1 }));

    [Fact]
    public void Validate_ConXlsxValido_EsValido()
    {
        var content = CreateXlsxLikeZip();

        var result = _validator.Validate("productos.xlsx", content.Length, content);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("productos.xls")]
    [InlineData("productos.csv")]
    [InlineData("productos.xlsx.exe")]
    public void Validate_ConExtensionNoPermitida_EsInvalido(string fileName)
    {
        var content = CreateXlsxLikeZip();

        var result = _validator.Validate(fileName, content.Length, content);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ConTextoRenombradoAXlsx_EsInvalido()
    {
        var content = new MemoryStream(Encoding.UTF8.GetBytes("esto no es un excel"));

        var result = _validator.Validate("falso.xlsx", content.Length, content);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ConZipQueNoEsExcel_EsInvalido()
    {
        var content = CreateZip("documento.txt");

        var result = _validator.Validate("comprimido.xlsx", content.Length, content);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ConArchivoMayorAlMaximo_EsInvalido()
    {
        var content = CreateXlsxLikeZip();

        var result = _validator.Validate("grande.xlsx", 2 * 1024 * 1024, content);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ConArchivoVacio_EsInvalido()
    {
        var result = _validator.Validate("vacio.xlsx", 0, new MemoryStream());

        Assert.False(result.IsValid);
    }

    private static MemoryStream CreateXlsxLikeZip() => CreateZip("xl/workbook.xml");

    private static MemoryStream CreateZip(string entryName)
    {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry(entryName);
            using var writer = new StreamWriter(entry.Open());
            writer.Write("<workbook/>");
        }

        stream.Position = 0;
        return stream;
    }
}
