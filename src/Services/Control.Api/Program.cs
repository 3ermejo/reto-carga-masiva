using Control.Api.Application.Common;
using Control.Api.Application.Interfaces;
using Control.Api.Application.Services;
using Control.Api.Application.Validation;
using Control.Api.Common;
using Control.Api.Extensions;
using Control.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Features;
using Npgsql;
using Shared.Messaging.RabbitMq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<UploadOptions>()
    .Bind(builder.Configuration.GetSection(UploadOptions.SectionName))
    .Validate(o => o.MaxFileSizeMB is > 0 and <= 100, "Upload:MaxFileSizeMB debe estar entre 1 y 100.")
    .ValidateOnStart();

var uploadOptions = builder.Configuration.GetSection(UploadOptions.SectionName).Get<UploadOptions>() ?? new UploadOptions();

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = uploadOptions.MaxRequestBodyBytes;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = uploadOptions.MaxRequestBodyBytes;
});

var connectionString = builder.Configuration.GetConnectionString("ControlDb");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("No se configuró la cadena de conexión 'ControlDb'.");

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationPolicies();
builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddRabbitMq(builder.Configuration);

builder.Services.AddScoped<ICargaRepository, CargaRepository>();
builder.Services.AddScoped<ICargaService, CargaService>();
builder.Services.AddSingleton<IExcelFileValidator, ExcelFileValidator>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
