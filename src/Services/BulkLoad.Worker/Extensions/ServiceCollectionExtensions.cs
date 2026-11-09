using BulkLoad.Worker.Application.Interfaces;
using BulkLoad.Worker.Infrastructure.Storage;

namespace BulkLoad.Worker.Extensions;

public static class ServiceCollectionExtensions
{
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
