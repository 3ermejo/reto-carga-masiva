namespace Shared.Messaging.Contracts;

/// <summary>
/// Cola "notificaciones". Lo publica BulkLoad.Worker cuando termina el proceso.
/// JSON: { "idCarga": 123, "usuario": "user@example.com", "fechaFin": "2025-02-10T10:20:00Z" }
/// </summary>
public record CargaFinalizadaMessage(int IdCarga, string Usuario, DateTime FechaFin);
