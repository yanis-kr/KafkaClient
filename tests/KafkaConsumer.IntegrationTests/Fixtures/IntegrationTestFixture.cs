using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Extensions;
using KafkaConsumer.Common.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace KafkaConsumer.IntegrationTests.Fixtures;

public class IntegrationTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }
    public IConfiguration Configuration { get; }
    public IHost Host { get; }

    public IntegrationTestFixture()
    {
        // Find the main project's appsettings.json
        var mainAppSettingsPath = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "src", "KafkaConsumer", "appsettings.json"));

        // If it doesn't exist, then fall back to test settings
        if (!File.Exists(mainAppSettingsPath))
        {
            mainAppSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.integration.json");
        }

        // Build test configuration
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile(mainAppSettingsPath, optional: false)
            // Allow test-specific configuration to override main settings
            .AddJsonFile("appsettings.integration.json", optional: true);

        Configuration = configBuilder.Build();

        // Create host with test configuration
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureServices((_, services) =>
            {
                // Add all the services from the real application
                services
                    .AddOptionsConfigurations(Configuration)
                    .RegisterEventHandlers()
                    .AddOktaAuthentication()
                    .AddApiClients(Configuration)
                    .AddSingleton<ITopicResolver, TopicResolver>();

                // Add test mocks and overrides
                RegisterTestOverrides(services);
            })
            .Build();

        ServiceProvider = Host.Services;
    }

    private void RegisterTestOverrides(IServiceCollection services)
    {
        // Replace external services with test doubles
        // For example, HTTP clients can be replaced with test implementations
        
        // HTTP clients are already registered, so we can replace them with test stubs
        // that don't make real HTTP calls
        services.AddHttpClient("test-http-client").ConfigurePrimaryHttpMessageHandler(() => 
            new TestHttpMessageHandler());
    }

    public void Dispose()
    {
        Host?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Test HTTP message handler that returns predefined responses instead of making real HTTP calls
/// </summary>
public class TestHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Return a success response by default
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"success\": true}")
        });
    }
}

// Collection fixture to share the test host across test classes in a collection
[CollectionDefinition("Integration tests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    // This class has no code, and is never created.
    // Its purpose is to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
} 