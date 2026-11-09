namespace Shared.Messaging.RabbitMq;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "rabbitmq";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>Nombre que se ve en el panel de RabbitMQ para identificar la conexion.</summary>
    public string ClientName { get; set; } = "carga-masiva";
}
