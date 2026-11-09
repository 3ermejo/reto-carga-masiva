using BulkLoad.Worker.Domain;

namespace BulkLoad.Worker.Application.Interfaces;

public interface IExcelReader
{
    /// <summary>Devuelve las filas con datos (las filas vacias se omiten).</summary>
    IReadOnlyList<FilaExcel> Leer(Stream contenido);
}
