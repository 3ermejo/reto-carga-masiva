namespace BulkLoad.Worker.Domain;

/// <summary>Registro limpio y validado, listo para insertarse en DataProcesada.</summary>
public record RegistroProducto(int Fila, string Periodo, string CodigoProducto, string NombreProducto, decimal Precio);
