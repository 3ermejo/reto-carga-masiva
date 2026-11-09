using System.Text;
using BulkLoad.Worker.Application.Exceptions;
using BulkLoad.Worker.Application.Services;
using BulkLoad.Worker.Infrastructure.Excel;
using Microsoft.Extensions.Options;

namespace BulkLoad.Worker.Tests;

public class ExcelReaderTests
{
    private readonly ExcelReader _reader = new(Options.Create(new ProcesamientoOptions { MaxFilas = 1000 }));

    public ExcelReaderTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Fact]
    public void Leer_ArchivoDelReto_DevuelveLas200Filas()
    {
        using var file = File.OpenRead(Path.Combine("samples", "carga_masiva_productos.xlsx"));

        var filas = _reader.Leer(file);

        Assert.Equal(200, filas.Count);
        Assert.Equal(2, filas[0].Fila);
        Assert.Equal("2025-03", filas[0].Periodo);
        Assert.Equal("P0117", filas[0].CodigoProducto);
    }

    [Fact]
    public void Leer_OmiteLasFilasVacias()
    {
        using var file = File.OpenRead(Path.Combine("samples", "productos_con_errores.xlsx"));

        var filas = _reader.Leer(file);

        Assert.Equal(12, filas.Count);
        Assert.DoesNotContain(filas, f => f.EstaVacia);
    }

    [Fact]
    public void Leer_ArchivoQueNoEsExcel_LanzaExcepcionDeFormato()
    {
        using var file = File.OpenRead(Path.Combine("samples", "no_es_excel.xlsx"));

        Assert.Throws<InvalidExcelException>(() => _reader.Leer(file));
    }
}
