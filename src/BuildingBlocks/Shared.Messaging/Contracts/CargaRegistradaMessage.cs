namespace Shared.Messaging.Contracts;

/// <summary>
/// Cola "carga_masiva". Lo publica Control.Api cuando el archivo ya esta en SeaweedFS.
/// JSON: { "idCarga": 123, "rutaArchivo": "seaweed://uploads/.../archivo.xlsx", "usuario": "user@example.com" }
/// </summary>
public record CargaRegistradaMessage(int IdCarga, string RutaArchivo, string Usuario);
