using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Extensions;
using KafkaConsumer.Common.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KafkaConsumer.IntegrationTests.Fixtures;

/// <summary>
/// Test fixture for KafkaConsumer that mimics the console application's service setup
/// </summary>
public class KafkaConsumerTestFixture : IDisposable
{
    /// <summary>
    /// Gets the service provider with all registered services
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Gets the configuration
    /// </summary>
    public IConfiguration Configuration { get; }

    /// <summary>
    /// Gets the host
    /// </summary>
    public IHost Host { get; }

    /// <summary>
    /// Creates a new instance of the test fixture with the application's configuration
    /// </summary>
    public KafkaConsumerTestFixture()
    {
        // In a test environment, we want to use the actual application's configuration
        var builder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                // Find the path to the main app's appsettings.json
                string projectDir = Directory.GetCurrentDirectory();
                string appSettingsPath = Path.GetFullPath(Path.Combine(
                    projectDir, "..", "..", "..", "..", "..", "src", "KafkaConsumer", "appsettings.json"));

                config.AddJsonFile(appSettingsPath, optional: false);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureServices((context, services) => 
            {
                // Configure services exactly like the real application
                services
                    .AddOptionsConfigurations(context.Configuration)
                    .RegisterEventHandlers()
                    .AddOktaAuthentication()
                    .AddApiClients(context.Configuration)
                    .AddSingleton<ITopicResolver, TopicResolver>();

                // Configure test overrides
                ConfigureTestServices(services);
            });

        Host = builder.Build();
        Services = Host.Services;
        Configuration = Services.GetRequiredService<IConfiguration>();
    }

    /// <summary>
    /// Override this method to configure test-specific services
    /// </summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        // This method can be overridden by derived fixtures to register test-specific services
        // For example, register mock implementations of external dependencies
    }

    /// <summary>
    /// Disposes the host
    /// </summary>
    public void Dispose()
    {
        Host?.Dispose();
        GC.SuppressFinalize(this);
    }
} 