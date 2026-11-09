using Control.Api.Application.Validation;

namespace Control.Api.Tests;

public class FileNameSanitizerTests
{
    [Theory]
    [InlineData(@"C:\Users\pablo\Desktop\productos.xlsx", "productos.xlsx")]
    [InlineData("../../etc/passwd.xlsx", "passwd.xlsx")]
    [InlineData("<script>alert(1)</script>.xlsx", "script_.xlsx")]
    [InlineData("carga de productos (marzo).xlsx", "carga de productos (marzo).xlsx")]
    [InlineData("", "archivo.xlsx")]
    public void Sanitize_DejaSoloUnNombreSeguro(string input, string expected)
    {
        Assert.Equal(expected, FileNameSanitizer.Sanitize(input));
    }
}
