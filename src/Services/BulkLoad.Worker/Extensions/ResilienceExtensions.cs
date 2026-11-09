using Polly;
using Polly.Retry;

namespace BulkLoad.Worker.Extensions;

public static class ResilienceExtensions
{
    public const string ProcesamientoPipeline = "procesamiento-carga";

    /// <summary>
    /// Patron de reintentos para el procesamiento completo de una carga (BD, RabbitMQ, SeaweedFS):
    /// 3 reintentos con espera exponencial (2s, 4s, 8s) y jitter.
    /// Es seguro reintentar porque fn_iniciar_proceso limpia lo que haya quedado a medias.
    /// </summary>
    public static IServiceCollection AddProcessingResilience(this IServiceCollection services)
    {
        services.AddResiliencePipeline(ProcesamientoPipeline, builder =>
        {
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not OperationCanceledException)
            });
        });

        return services;
    }
}
