using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;

namespace KafkaConsumer.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterEventHandlers(this IServiceCollection services)
    {
        // Discover and register all IEventHandler implementations
        Assembly assembly = Assembly.GetExecutingAssembly();
        var handlerTypes = assembly.GetTypes().Where(t =>
            typeof(IEventHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        foreach (var type in handlerTypes)
        {
            services.AddSingleton(typeof(IEventHandler), type);
        }
        return services;
    }

    public static IServiceCollection AddOptionsConfigurations(this IServiceCollection services, 
        IConfiguration config)
    {
        services.Configure<TopicSettings>(config.GetSection("TopicConfigurations"));
        services.Configure<KafkaSettings>(config.GetSection("Kafka"));
        return services;
    }
} 