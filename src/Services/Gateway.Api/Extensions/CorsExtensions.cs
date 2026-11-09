namespace Gateway.Api.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "web-client";

    public static IServiceCollection AddClientCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            // Solo el origen del cliente React, y solo los metodos y headers que usa.
            // AllowCredentials permite que el navegador envie la cookie del refresh token.
            options.AddPolicy(PolicyName, policy => policy
                .WithOrigins(allowedOrigins)
                .WithMethods("GET", "POST")
                .WithHeaders("Authorization", "Content-Type")
                .WithExposedHeaders("Content-Disposition")
                .AllowCredentials());
        });

        return services;
    }
}
