using Notification.Worker.Application.Models;

namespace Notification.Worker.Application.Interfaces;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
