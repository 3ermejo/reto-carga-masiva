namespace Gateway.Api.Extensions;

public static class SecurityHeadersExtensions
{
    /// <summary>
    /// Agrega headers de seguridad a todas las respuestas. Se usa OnStarting para
    /// que se apliquen despues de que YARP copie los headers del servicio destino.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "no-referrer";
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
                headers["Cache-Control"] = "no-store";
                headers.Remove("Server");
                headers.Remove("X-Powered-By");
                return Task.CompletedTask;
            });

            await next(context);
        });
    }
}
