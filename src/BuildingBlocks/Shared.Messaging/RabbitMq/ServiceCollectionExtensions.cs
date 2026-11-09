using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shared.Messaging.RabbitMq;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Host), "Falta configurar RabbitMq:Host.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Username) && !string.IsNullOrWhiteSpace(o.Password),
                "Falta configurar RabbitMq:Username y RabbitMq:Password.")
            .ValidateOnStart();

        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
        return services;
    }

    /// <summary>Registra el handler y el servicio que escucha la cola.</summary>
    public static IServiceCollection AddRabbitMqConsumer<TMessage, THandler>(this IServiceCollection services, string queueName)
        where THandler : class, IMessageHandler<TMessage>
    {
        services.AddScoped<IMessageHandler<TMessage>, THandler>();
        services.AddHostedService(provider => new RabbitMqConsumerService<TMessage>(
            provider.GetRequiredService<RabbitMqConnection>(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<RabbitMqConsumerService<TMessage>>>(),
            queueName));

        return services;
    }
}
