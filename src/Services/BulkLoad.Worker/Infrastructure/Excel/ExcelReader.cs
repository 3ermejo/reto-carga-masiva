using System.Globalization;
using System.Text;
using BulkLoad.Worker.Application.Exceptions;
using BulkLoad.Worker.Application.Interfaces;
using BulkLoad.Worker.Application.Services;
using BulkLoad.Worker.Domain;
using ExcelDataReader;
using Microsoft.Extensions.Options;

namespace BulkLoad.Worker.Infrastructure.Excel;

/// <summary>
/// Lee la primera hoja del Excel fila por fila con ExcelDataReader (no carga todo el libro en objetos).
/// Las columnas se ubican por el nombre de la cabecera, sin importar el orden, mayusculas o tildes.
/// </summary>
public class ExcelReader : IExcelReader
{
    public const string ColumnaPeriodo = "periodo";
    public const string ColumnaCodigo = "codigoproducto";
    public const string ColumnaNombre = "nombreproducto";
    public const string ColumnaPrecio = "precio";

    private static readonly string[] ColumnasRequeridas = [ColumnaPeriodo, ColumnaCodigo, ColumnaNombre, ColumnaPrecio];

    private readonly int _maxFilas;

    public ExcelReader(IOptions<ProcesamientoOptions> options)
    {
        _maxFilas = options.Value.MaxFilas;
    }

    public IReadOnlyList<FilaExcel> Leer(Stream contenido)
    {
        try
        {
            using var reader = ExcelReaderFactory.CreateOpenXmlReader(contenido);
            return LeerFilas(reader);
        }
        catch (InvalidExcelException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidExcelException("El archivo no es un Excel (.xlsx) válido o está dañado.", ex);
        }
    }

    private List<FilaExcel> LeerFilas(IExcelDataReader reader)
    {
        var filas = new List<FilaExcel>();
        Dictionary<string, int>? columnas = null;
        var numeroFila = 0;

        while (reader.Read())
        {
            numeroFila++;

            if (columnas is null)
            {
                // La primera fila con datos es la cabecera
                if (EsFilaVacia(reader))
                    continue;

                columnas = LeerCabecera(reader);
                continue;
            }

            var fila = new FilaExcel(
                numeroFila,
                Celda(reader, columnas[ColumnaPeriodo]),
                Celda(reader, columnas[ColumnaCodigo]),
                Celda(reader, columnas[ColumnaNombre]),
                Celda(reader, columnas[ColumnaPrecio]));

            // Las filas vacias no se registran
            if (fila.EstaVacia)
                continue;

            if (filas.Count >= _maxFilas)
                throw new InvalidExcelException($"El archivo supera el máximo de {_maxFilas} filas permitido.");

            filas.Add(fila);
        }

        if (columnas is null)
            throw new InvalidExcelException("El archivo no contiene datos.");

        return filas;
    }

    private static Dictionary<string, int> LeerCabecera(IExcelDataReader reader)
    {
        var columnas = new Dictionary<string, int>();

        for (var i = 0; i < reader.FieldCount; i++)
        {
            var nombre = Normalizar(FilaExcel.ToText(reader.GetValue(i)));
            if (nombre is not null)
                columnas.TryAdd(nombre, i);
        }

        var faltantes = ColumnasRequeridas.Where(c => !columnas.ContainsKey(c)).ToList();
        if (faltantes.Count > 0)
        {
            throw new InvalidExcelException(
                "El archivo no tiene las columnas requeridas: Periodo, CodigoProducto, NombreProducto, Precio.");
        }

        return columnas;
    }

    private static object? Celda(IExcelDataReader reader, int indice)
    {
        return indice < reader.FieldCount ? reader.GetValue(indice) : null;
    }

    private static bool EsFilaVacia(IExcelDataReader reader)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (FilaExcel.ToText(reader.GetValue(i)) is not null)
                return false;
        }

        return true;
    }

    // "Código Producto" -> "codigoproducto"
    private static string? Normalizar(string? texto)
    {
        if (texto is null)
            return null;

        var sinTildes = new StringBuilder();
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c))
                sinTildes.Append(char.ToLowerInvariant(c));
        }

        return sinTildes.ToString();
    }
}
