using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Services;
using KafkaConsumer.Features.UpdateOrder.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Refit;
using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;

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
        services.Configure<OktaSettings>(config.GetSection("Okta"));
        return services;
    }
    
    public static IServiceCollection AddOktaAuthentication(this IServiceCollection services)
    {
        // Register the Okta token API client
        services.AddRefitClient<IOktaTokenApi>()
            .ConfigureHttpClient((sp, client) => 
            {
                var oktaSettings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OktaSettings>>().Value;
                client.BaseAddress = new Uri(oktaSettings.TokenUrl);
            })
            .AddPolicyHandler(GetRetryPolicy());
            
        // Register the token provider as a singleton
        services.AddSingleton<IOktaTokenProvider, OktaTokenProvider>();
        
        // Register the auth handler as a transient service
        services.AddTransient<OktaAuthenticationHandler>();
        
        return services;
    }
    
    public static IServiceCollection AddApiClients(this IServiceCollection services, IConfiguration config)
    {
        // Get external systems configuration
        var externalSystems = config.GetSection("ExternalSystems")
            .Get<ExternalSystemsSettings>();
            
        if (externalSystems == null)
        {
            throw new InvalidOperationException("ExternalSystems configuration is missing");
        }
        
        // Find the Order API configuration
        var orderApiDef = externalSystems.ApiDefinitions
            .FirstOrDefault(a => a.Name.Equals("OrderApi", StringComparison.OrdinalIgnoreCase));
            
        if (orderApiDef == null)
        {
            throw new InvalidOperationException("OrderApi configuration is missing in ExternalSystems.ApiDefinitions");
        }
        
        // Register the order API client
        services.AddRefitClient<IOrderApi>()
            .ConfigureHttpClient(client => 
            {
                client.BaseAddress = new Uri(orderApiDef.BaseUrl);
            })
            .AddHttpMessageHandler<OktaAuthenticationHandler>()
            .AddPolicyHandler(GetRetryPolicy());
            
        return services;
    }
    
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
} 