namespace Notification.Worker.Infrastructure.Email;

/// <summary>Configurable por variables de entorno (Smtp__Host, Smtp__Port, ...).</summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 25;

    // En local (Mailpit) va en false; en produccion debe ir en true
    public bool UseStartTls { get; set; }

    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Carga Masiva";
}
