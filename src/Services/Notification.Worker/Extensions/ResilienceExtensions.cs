using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Notification.Worker.Extensions;

public static class ResilienceExtensions
{
    public const string SmtpPipeline = "smtp";

    /// <summary>
    /// Reintentos (3, con espera exponencial) + Circuit Breaker para el servidor de correo:
    /// si en 30 segundos falla la mitad de los envios (minimo 4), el circuito se abre 30 segundos.
    /// </summary>
    public static IServiceCollection AddSmtpResilience(this IServiceCollection services)
    {
        services.AddResiliencePipeline(SmtpPipeline, builder =>
        {
            builder
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(2),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not OperationCanceledException and not BrokenCircuitException)
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    MinimumThroughput = 4,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(30)
                });
        });

        return services;
    }
}
