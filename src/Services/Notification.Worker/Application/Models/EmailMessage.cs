namespace Notification.Worker.Application.Models;

public record EmailMessage(string ToAddress, string Subject, string HtmlBody, string TextBody);
