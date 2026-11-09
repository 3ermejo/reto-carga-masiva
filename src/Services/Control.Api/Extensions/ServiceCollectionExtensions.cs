using System.Text;
using Control.Api.Application.Interfaces;
using Control.Api.Domain;
using Control.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Control.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string PuedeCargarPolicy = "PuedeCargar";

    /// <summary>
    /// Control.Api valida el JWT aunque el Gateway ya lo hizo (defensa en profundidad):
    /// si alguien llegara directo al servicio, igual necesita un token valido.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var key = configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("Falta configurar Jwt:Issuer y Jwt:Audience.");

        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });

        return services;
    }

    /// <summary>Solo los roles Administrador y Operador pueden ejecutar cargas masivas.</summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(PuedeCargarPolicy, policy => policy.RequireRole(Roles.Administrador, Roles.Operador));

        return services;
    }

    /// <summary>
    /// Cliente HTTP para SeaweedFS con el pipeline de resiliencia estandar de .NET (Polly):
    /// reintentos con backoff exponencial, circuit breaker y timeouts.
    /// </summary>
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var filerUrl = configuration["SeaweedFs:FilerUrl"];
        if (string.IsNullOrWhiteSpace(filerUrl))
            throw new InvalidOperationException("Falta configurar SeaweedFs:FilerUrl.");

        services.AddHttpClient<IFileStorage, SeaweedFsStorage>(client =>
            {
                client.BaseAddress = new Uri(filerUrl.TrimEnd('/') + "/");
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(2);
            });

        return services;
    }
}
