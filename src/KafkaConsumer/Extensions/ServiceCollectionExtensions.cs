using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;

namespace KafkaConsumer.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterEventHandlers(this IServiceCollection services)
    {
        // Discover and register all IEventHandler implementations
        Assembly assembly = Assembly.GetExecutingAssembly();
        var handlerTypes = assembly.GetTypes().Where(t =>
            typeof(KafkaConsumer.Common.Contracts.IEventHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        foreach (var type in handlerTypes)
        {
            services.AddSingleton(typeof(KafkaConsumer.Common.Contracts.IEventHandler), type);
        }
        return services;
    }

    public static IServiceCollection AddOptionsConfigurations(this IServiceCollection services, 
        IConfiguration config)
    {
        services.Configure<KafkaConsumer.Common.Configuration.TopicConfigurations>(config.GetSection("TopicConfigurations"));
        services.Configure<KafkaConsumer.Common.Configuration.KafkaSettings>(config.GetSection("Kafka"));
        return services;
    }
} 