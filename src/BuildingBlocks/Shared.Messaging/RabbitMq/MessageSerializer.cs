using System.Text.Json;

namespace Shared.Messaging.RabbitMq;

public static class MessageSerializer
{
    // camelCase: { "idCarga": 123, "rutaArchivo": "...", "usuario": "..." }
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
