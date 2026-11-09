using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Notification.Worker.Application.Interfaces;
using Notification.Worker.Application.Models;
using Notification.Worker.Extensions;
using Polly.Registry;

namespace Notification.Worker.Infrastructure.Email;

/// <summary>
/// Envio de correos con MailKit. El envio pasa por un pipeline de Polly con
/// reintentos y circuit breaker: si el servidor SMTP falla seguido, se deja
/// de intentar por un tiempo en lugar de saturarlo.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ResiliencePipelineProvider<string> pipelineProvider)
    {
        _options = options.Value;
        _pipelineProvider = pipelineProvider;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(message.ToAddress));
        mimeMessage.Subject = message.Subject;
        mimeMessage.Body = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody
        }.ToMessageBody();

        var pipeline = _pipelineProvider.GetPipeline(ResilienceExtensions.SmtpPipeline);
        await pipeline.ExecuteAsync(async token => await SendInternalAsync(mimeMessage, token), cancellationToken);
    }

    private async Task SendInternalAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient();
        var socketOptions = _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

        await client.ConnectAsync(_options.Host, _options.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.Username))
            await client.AuthenticateAsync(_options.Username, _options.Password ?? string.Empty, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
