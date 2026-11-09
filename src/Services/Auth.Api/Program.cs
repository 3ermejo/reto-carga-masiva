using Auth.Api.Application.Interfaces;
using Auth.Api.Application.Services;
using Auth.Api.Common;
using Auth.Api.Infrastructure.Persistence;
using Auth.Api.Infrastructure.Security;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    // Login y refresh solo reciben un JSON pequeño
    options.Limits.MaxRequestBodySize = 32 * 1024;
});

var connectionString = builder.Configuration.GetConnectionString("AuthDb");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("No se configuró la cadena de conexión 'AuthDb'.");

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<LockoutOptions>()
    .Bind(builder.Configuration.GetSection(LockoutOptions.SectionName));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
