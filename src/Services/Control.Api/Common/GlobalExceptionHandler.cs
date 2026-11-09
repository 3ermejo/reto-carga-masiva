using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Control.Api.Common;

/// <summary>
/// Manejo global de excepciones: respuesta generica sin stack trace ni detalles internos.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            BadHttpRequestException badRequest => new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge
                    ? "El archivo supera el tamaño máximo permitido."
                    : "La solicitud no es válida."
            },
            UnauthorizedAccessException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "No autorizado."
            },
            FileNotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "El archivo no se encuentra disponible."
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Ocurrió un error inesperado. Intente nuevamente más tarde."
            }
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (problem.Status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Error no controlado en {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            _logger.LogWarning("Solicitud rechazada en {Path}: {Message}", httpContext.Request.Path, exception.Message);

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
