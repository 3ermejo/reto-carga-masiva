using System.Text;
using BulkLoad.Worker.Application.Interfaces;
using BulkLoad.Worker.Application.Services;
using BulkLoad.Worker.Application.Validation;
using BulkLoad.Worker.Extensions;
using BulkLoad.Worker.Infrastructure.Excel;
using BulkLoad.Worker.Infrastructure.Persistence;
using Npgsql;
using Shared.Messaging.Contracts;
using Shared.Messaging.RabbitMq;

var builder = Host.CreateApplicationBuilder(args);

// ExcelDataReader necesita las codificaciones de Windows en .NET
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var connectionString = builder.Configuration.GetConnectionString("BulkLoadDb");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("No se configuró la cadena de conexión 'BulkLoadDb'.");

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));

builder.Services.AddOptions<ProcesamientoOptions>()
    .Bind(builder.Configuration.GetSection(ProcesamientoOptions.SectionName));

builder.Services.AddScoped<ICargaRepository, CargaRepository>();
builder.Services.AddScoped<ICargaProcessor, CargaProcessor>();
builder.Services.AddSingleton<IExcelReader, ExcelReader>();
builder.Services.AddSingleton<RegistroValidator>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddProcessingResilience();

// Consumidor de la cola carga_masiva y publicador hacia la cola notificaciones
builder.Services.AddRabbitMq(builder.Configuration);
builder.Services.AddRabbitMqConsumer<CargaRegistradaMessage, CargaRegistradaHandler>(RabbitMqTopology.CargaMasivaQueue);

var host = builder.Build();
host.Run();
