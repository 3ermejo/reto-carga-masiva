using BulkLoad.Worker.Application.Validation;
using BulkLoad.Worker.Domain;

namespace BulkLoad.Worker.Tests;

public class RegistroValidatorTests
{
    private const string PeriodoActual = "2026-11";
    private readonly RegistroValidator _validator = new();

    [Fact]
    public void Validar_FilaCorrecta_DevuelveElRegistroLimpio()
    {
        var result = _validator.Validar(new FilaExcel(2, "2025-03", " p0117 ", "  Producto   P0117 ", 418.27), PeriodoActual);

        Assert.True(result.EsValido);
        Assert.Equal("P0117", result.Registro!.CodigoProducto);
        Assert.Equal("Producto P0117", result.Registro.NombreProducto);
        Assert.Equal(418.27m, result.Registro.Precio);
        Assert.Equal("2025-03", result.Registro.Periodo);
    }

    [Fact]
    public void Validar_CeldasVacias_UsaValoresPorDefecto()
    {
        var result = _validator.Validar(new FilaExcel(3, null, "P0001", null, null), PeriodoActual);

        Assert.True(result.EsValido);
        Assert.Equal(PeriodoActual, result.Registro!.Periodo);
        Assert.Equal(RegistroValidator.NombrePorDefecto, result.Registro.NombreProducto);
        Assert.Equal(RegistroValidator.PrecioPorDefecto, result.Registro.Precio);
    }

    [Theory]
    [InlineData("202503", "2025-03")]
    [InlineData("2025/03", "2025-03")]
    [InlineData("2025-12", "2025-12")]
    public void Validar_NormalizaElPeriodo(string periodo, string esperado)
    {
        var result = _validator.Validar(new FilaExcel(2, periodo, "P1", "Producto", 1d), PeriodoActual);

        Assert.Equal(esperado, result.Registro!.Periodo);
    }

    [Fact]
    public void Validar_PeriodoComoFechaDeExcel_LoConvierte()
    {
        var result = _validator.Validar(new FilaExcel(2, new DateTime(2025, 3, 1), "P1", "Producto", 1d), PeriodoActual);

        Assert.Equal("2025-03", result.Registro!.Periodo);
    }

    [Theory]
    [InlineData("12,50", 12.50)]
    [InlineData("33.333", 33.33)]
    [InlineData("10", 10)]
    public void Validar_LimpiaElPrecio(string precio, double esperado)
    {
        var result = _validator.Validar(new FilaExcel(2, "2025-01", "P1", "Producto", precio), PeriodoActual);

        Assert.Equal((decimal)esperado, result.Registro!.Precio);
    }

    [Theory]
    [InlineData("2025-01", null, "Producto", 10d)]          // codigo vacio
    [InlineData("2025-01", "P 01", "Producto", 10d)]        // codigo con espacio
    [InlineData("2025-13", "P01", "Producto", 10d)]         // mes invalido
    [InlineData("marzo", "P01", "Producto", 10d)]           // periodo invalido
    [InlineData("2025-01", "P01", "Producto", -5d)]         // precio negativo
    [InlineData("2025-01", "P01", "=HYPERLINK(\"x\")", 1d)] // inyeccion de formula
    [InlineData("2025-01", "P01", "@SUMA(A1)", 1d)]         // inyeccion de formula
    public void Validar_DatosInvalidos_DevuelveError(string? periodo, string? codigo, string nombre, double precio)
    {
        var result = _validator.Validar(new FilaExcel(2, periodo, codigo, nombre, precio), PeriodoActual);

        Assert.False(result.EsValido);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Fact]
    public void Validar_PrecioNoNumerico_DevuelveError()
    {
        var result = _validator.Validar(new FilaExcel(2, "2025-01", "P01", "Producto", "abc"), PeriodoActual);

        Assert.False(result.EsValido);
    }
}
