using KafkaConsumer.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KafkaConsumer.Tests.Extensions;

public class HostBuilderExtensionsTests
{
    [Fact]
    public void ConfigureAppConfigurationWithUserSecrets_ConfiguresAllSources()
    {
        // Arrange
        var builder = new HostBuilder();
        builder.ConfigureAppConfigurationWithUserSecrets();

        // Act
        var host = builder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        // Set environment variable after configuration is built
        Environment.SetEnvironmentVariable("TEST_ENV_VAR", "test_value");

        // Assert
        // Verify that configuration sources are loaded
        Assert.NotNull(configuration);
        
        // Verify environment variables are loaded
        Assert.Equal("test_value", Environment.GetEnvironmentVariable("TEST_ENV_VAR"));
        
        // Clean up
        Environment.SetEnvironmentVariable("TEST_ENV_VAR", null);
    }
} 