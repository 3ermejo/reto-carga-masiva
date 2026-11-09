using System.Globalization;
using System.Text.RegularExpressions;
using BulkLoad.Worker.Domain;

namespace BulkLoad.Worker.Application.Validation;

/// <summary>
/// Limpieza y validacion de cada fila del Excel.
/// Reglas de limpieza: se quitan espacios, el codigo se pasa a mayusculas,
/// el periodo se normaliza a YYYY-MM y el precio se redondea a 2 decimales.
/// Valores por defecto cuando la celda esta vacia:
///   Periodo -> periodo de la fecha de carga, NombreProducto -> "SIN NOMBRE", Precio -> 0.
/// CodigoProducto es la clave del registro: si esta vacio la fila se reporta como invalida.
/// </summary>
public class RegistroValidator
{
    public const string NombrePorDefecto = "SIN NOMBRE";
    public const decimal PrecioPorDefecto = 0m;
    public const int MaxLongitudNombre = 200;
    public const decimal PrecioMaximo = 999_999_999_999.99m;

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);
    private static readonly Regex CodigoPattern = new("^[A-Z0-9-]{1,50}$", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex PeriodoPattern = new(@"^(\d{4})[-/]?(0[1-9]|1[0-2])$", RegexOptions.Compiled, RegexTimeout);
    private static readonly Regex Espacios = new(@"\s+", RegexOptions.Compiled, RegexTimeout);

    // Caracteres con los que Excel interpreta una celda como formula (CSV/Excel injection)
    private static readonly char[] PrefijosFormula = ['=', '+', '-', '@'];

    public ResultadoValidacion Validar(FilaExcel fila, string periodoPorDefecto)
    {
        var errores = new List<string>();

        var periodo = LimpiarPeriodo(fila.Periodo, periodoPorDefecto, errores);
        var codigo = LimpiarCodigo(fila.CodigoProducto, errores);
        var nombre = LimpiarNombre(fila.NombreProducto, errores);
        var precio = LimpiarPrecio(fila.Precio, errores);

        if (errores.Count > 0)
            return ResultadoValidacion.Invalido(string.Join(" ", errores));

        return ResultadoValidacion.Valido(new RegistroProducto(fila.Fila, periodo!, codigo!, nombre!, precio));
    }

    private static string? LimpiarPeriodo(object? valor, string periodoPorDefecto, List<string> errores)
    {
        if (valor is DateTime fecha)
            return fecha.ToString("yyyy-MM", CultureInfo.InvariantCulture);

        var texto = FilaExcel.ToText(valor);
        if (texto is null)
            return periodoPorDefecto;

        var match = PeriodoPattern.Match(texto);
        if (!match.Success)
        {
            errores.Add($"Periodo '{texto}' no válido (formato esperado YYYY-MM).");
            return null;
        }

        return $"{match.Groups[1].Value}-{match.Groups[2].Value}";
    }

    private static string? LimpiarCodigo(object? valor, List<string> errores)
    {
        var codigo = FilaExcel.ToText(valor)?.ToUpperInvariant();

        if (codigo is null)
        {
            errores.Add("CodigoProducto es obligatorio.");
            return null;
        }

        if (!CodigoPattern.IsMatch(codigo))
        {
            errores.Add("CodigoProducto solo admite letras, números y guiones (máximo 50).");
            return null;
        }

        return codigo;
    }

    private static string? LimpiarNombre(object? valor, List<string> errores)
    {
        var texto = FilaExcel.ToText(valor);
        if (texto is null)
            return NombrePorDefecto;

        var nombre = Espacios.Replace(texto, " ");

        if (nombre.Length > MaxLongitudNombre)
        {
            errores.Add($"NombreProducto supera los {MaxLongitudNombre} caracteres.");
            return null;
        }

        if (PrefijosFormula.Contains(nombre[0]))
        {
            errores.Add("NombreProducto no puede empezar con =, +, - o @.");
            return null;
        }

        return nombre;
    }

    private static decimal LimpiarPrecio(object? valor, List<string> errores)
    {
        decimal precio;

        switch (valor)
        {
            case null:
                return PrecioPorDefecto;
            case double numero when double.IsFinite(numero) && Math.Abs(numero) <= (double)PrecioMaximo:
                precio = (decimal)numero;
                break;
            case decimal numero:
                precio = numero;
                break;
            case int numero:
                precio = numero;
                break;
            default:
                var texto = FilaExcel.ToText(valor);
                if (texto is null)
                    return PrecioPorDefecto;

                // Limpieza: se acepta coma decimal (12,50 -> 12.50)
                texto = texto.Replace(" ", string.Empty);
                if (texto.Contains(',') && !texto.Contains('.'))
                    texto = texto.Replace(',', '.');

                if (!decimal.TryParse(texto, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out precio))
                {
                    errores.Add($"Precio '{texto}' no es un número válido.");
                    return 0;
                }
                break;
        }

        if (precio < 0)
        {
            errores.Add("Precio no puede ser negativo.");
            return 0;
        }

        if (precio > PrecioMaximo)
        {
            errores.Add("Precio supera el valor máximo permitido.");
            return 0;
        }

        return Math.Round(precio, 2, MidpointRounding.AwayFromZero);
    }
}
