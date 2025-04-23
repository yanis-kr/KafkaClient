using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Options;

namespace KafkaConsumer.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterEventHandlers(this IServiceCollection services)
    {
        // Discover and register all IEventHandler implementations from all loaded assemblies
        var handlerTypes = System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IEventHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in handlerTypes)
        {
            // Register both the concrete type and the interface
            services.AddScoped(type);
            services.AddScoped(typeof(IEventHandler), type);
        }

        return services;
    }

    public static IServiceCollection AddOptionsConfigurations(this IServiceCollection services, 
        IConfiguration config)
    {
        services.Configure<ExternalSystemsSettings>(config.GetSection("ExternalSystems"));
        services.Configure<TopicSettings>(config.GetSection("TopicConfigurations"));
        services.Configure<KafkaSettings>(config.GetSection("Kafka"));

        // Register SchemaRegistry client
        services.AddSingleton<ISchemaRegistryClient>(sp =>
        {
            var kafkaSettings = sp.GetRequiredService<IOptions<KafkaSettings>>().Value;
            var schemaRegistryConfig = new SchemaRegistryConfig
            {
                Url = kafkaSettings.SchemaRegistryUrl
            };

            if (!string.IsNullOrEmpty(kafkaSettings.SchemaRegistryAuthKey) && 
                !string.IsNullOrEmpty(kafkaSettings.SchemaRegistryAuthSecret))
            {
                //schemaRegistryConfig.BasicAuthCredentialsInfo = new BasicAuthCredentialsInfo(
                //    kafkaSettings.SchemaRegistryAuthKey,
                //    kafkaSettings.SchemaRegistryAuthSecret);
            }

            return new CachedSchemaRegistryClient(schemaRegistryConfig);
        });

        return services;
    }
} 