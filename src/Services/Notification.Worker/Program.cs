using Notification.Worker.Application.Interfaces;
using Notification.Worker.Application.Services;
using Notification.Worker.Extensions;
using Notification.Worker.Infrastructure.Email;
using Notification.Worker.Infrastructure.Persistence;
using Npgsql;
using Shared.Messaging.Contracts;
using Shared.Messaging.RabbitMq;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("NotificationDb");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("No se configuró la cadena de conexión 'NotificationDb'.");

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));

builder.Services.AddOptions<SmtpOptions>()
    .Bind(builder.Configuration.GetSection(SmtpOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Host) && o.Port > 0, "Falta configurar Smtp:Host y Smtp:Port.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.FromAddress), "Falta configurar Smtp:FromAddress.")
    .ValidateOnStart();

builder.Services.AddScoped<ICargaRepository, CargaRepository>();
builder.Services.AddSingleton<IEmailTemplateBuilder, EmailTemplateBuilder>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddSmtpResilience();

// Consumidor de la cola notificaciones
builder.Services.AddRabbitMq(builder.Configuration);
builder.Services.AddRabbitMqConsumer<CargaFinalizadaMessage, CargaFinalizadaHandler>(RabbitMqTopology.NotificacionesQueue);

var host = builder.Build();
host.Run();
