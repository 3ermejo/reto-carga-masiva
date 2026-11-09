using Notification.Worker.Application.Models;
using Notification.Worker.Domain;

namespace Notification.Worker.Application.Interfaces;

public interface IEmailTemplateBuilder
{
    EmailMessage Build(CargaNotificacion carga, string destinatario);
}
