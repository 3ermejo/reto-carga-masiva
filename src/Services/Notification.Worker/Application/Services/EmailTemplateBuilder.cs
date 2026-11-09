using System.Net;
using System.Text;
using Notification.Worker.Application.Interfaces;
using Notification.Worker.Application.Models;
using Notification.Worker.Domain;
using Shared.Messaging.Contracts;

namespace Notification.Worker.Application.Services;

/// <summary>
/// Arma el correo de resultado. Todo dato que viene del usuario (nombre del archivo, mensajes)
/// se codifica con HtmlEncode para evitar inyeccion de HTML en el correo.
/// </summary>
public class EmailTemplateBuilder : IEmailTemplateBuilder
{
    public EmailMessage Build(CargaNotificacion carga, string destinatario)
    {
        var resultado = TextoResultado(carga.Resultado);
        var subject = $"Carga masiva #{carga.Id} finalizada: {resultado}";

        return new EmailMessage(destinatario, subject, BuildHtml(carga, resultado), BuildText(carga, resultado));
    }

    private static string BuildHtml(CargaNotificacion carga, string resultado)
    {
        var html = new StringBuilder();
        html.Append("<div style=\"font-family:Arial,sans-serif;font-size:14px;color:#222\">");
        html.Append("<p>Hola,</p>");
        html.Append($"<p>Terminó el procesamiento del archivo <strong>{Encode(carga.NombreArchivo)}</strong> (carga #{carga.Id}).</p>");
        html.Append("<table cellpadding=\"6\" style=\"border-collapse:collapse\">");
        html.Append($"<tr><td>Resultado</td><td><strong>{resultado}</strong></td></tr>");
        html.Append($"<tr><td>Periodos</td><td>{Encode(carga.Periodos ?? "-")}</td></tr>");
        html.Append($"<tr><td>Registros leídos</td><td>{carga.TotalRegistros}</td></tr>");
        html.Append($"<tr><td>Registros insertados</td><td>{carga.RegistrosInsertados}</td></tr>");
        html.Append($"<tr><td>Registros no cargados</td><td>{carga.RegistrosFallidos}</td></tr>");
        html.Append($"<tr><td>Fecha de fin</td><td>{FormatoFecha(carga.FechaFin)}</td></tr>");
        html.Append("</table>");

        if (!string.IsNullOrWhiteSpace(carga.Mensaje))
            html.Append($"<p>Detalle: {Encode(carga.Mensaje)}</p>");

        html.Append("<p>Puede revisar el detalle de los registros en la aplicación.</p>");
        html.Append("</div>");
        return html.ToString();
    }

    private static string BuildText(CargaNotificacion carga, string resultado)
    {
        var text = new StringBuilder();
        text.AppendLine("Hola,");
        text.AppendLine();
        text.AppendLine($"Terminó el procesamiento del archivo {carga.NombreArchivo} (carga #{carga.Id}).");
        text.AppendLine($"Resultado: {resultado}");
        text.AppendLine($"Periodos: {carga.Periodos ?? "-"}");
        text.AppendLine($"Registros leídos: {carga.TotalRegistros}");
        text.AppendLine($"Registros insertados: {carga.RegistrosInsertados}");
        text.AppendLine($"Registros no cargados: {carga.RegistrosFallidos}");
        text.AppendLine($"Fecha de fin: {FormatoFecha(carga.FechaFin)}");

        if (!string.IsNullOrWhiteSpace(carga.Mensaje))
            text.AppendLine($"Detalle: {carga.Mensaje}");

        return text.ToString();
    }

    private static string TextoResultado(string? resultado) => resultado switch
    {
        ResultadoCarga.Exitoso => "Exitosa",
        ResultadoCarga.ConObservaciones => "Con observaciones",
        ResultadoCarga.Rechazado => "Rechazada",
        _ => "Con error"
    };

    private static string FormatoFecha(DateTime? fecha) =>
        fecha.HasValue ? fecha.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'") : "-";

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
