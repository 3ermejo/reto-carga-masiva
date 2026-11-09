using System.Threading.RateLimiting;

namespace Gateway.Api.Extensions;

/// <summary>
/// Patron Rate Limiting: ventana fija por IP.
/// - General: protege a todos los servicios de un exceso de peticiones.
/// - Auth: limite estricto para /auth (fuerza bruta sobre el login).
/// </summary>
public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var authLimit = configuration.GetValue("RateLimiting:AuthPermitLimit", 10);
        var generalLimit = configuration.GetValue("RateLimiting:GeneralPermitLimit", 120);
        var window = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:WindowSeconds", 60));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(GetClientIp(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = generalLimit,
                    Window = window,
                    QueueLimit = 0
                }));

            options.AddPolicy(AuthPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(GetClientIp(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = authLimit,
                    Window = window,
                    QueueLimit = 0
                }));

            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                return ValueTask.CompletedTask;
            };
        });

        return services;
    }

    private static string GetClientIp(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
