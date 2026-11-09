namespace BulkLoad.Worker.Application.Exceptions;

/// <summary>El archivo no se puede leer como Excel o no tiene las columnas requeridas. No se reintenta.</summary>
public class InvalidExcelException : Exception
{
    public InvalidExcelException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
