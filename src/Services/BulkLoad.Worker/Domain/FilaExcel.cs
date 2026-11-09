using System.Globalization;

namespace BulkLoad.Worker.Domain;

/// <summary>Fila tal como viene del Excel (valores sin limpiar). Fila = numero de fila en la hoja.</summary>
public record FilaExcel(int Fila, object? Periodo, object? CodigoProducto, object? NombreProducto, object? Precio)
{
    public bool EstaVacia =>
        IsEmpty(Periodo) && IsEmpty(CodigoProducto) && IsEmpty(NombreProducto) && IsEmpty(Precio);

    /// <summary>Datos originales para guardarlos en la auditoria de fallidos.</summary>
    public IReadOnlyDictionary<string, string?> ADiccionario() => new Dictionary<string, string?>
    {
        ["Periodo"] = ToText(Periodo),
        ["CodigoProducto"] = ToText(CodigoProducto),
        ["NombreProducto"] = ToText(NombreProducto),
        ["Precio"] = ToText(Precio)
    };

    public static string? ToText(object? value)
    {
        var text = value switch
        {
            null => null,
            string s => s,
            DateTime d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };

        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static bool IsEmpty(object? value) => ToText(value) is null;
}
