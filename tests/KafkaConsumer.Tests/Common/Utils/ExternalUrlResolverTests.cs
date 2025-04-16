using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Utls;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace KafkaConsumer.Tests.Common.Utils;

public class ExternalUrlResolverTests
{
    //true test tied to current appsettings.json values, thus fragile
    [Fact]
    public void GetAbsoluteUrl_Real_AppSettings_ReturnsExpectedUrl()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // points to test project's output folder
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var settings = new ExternalSystemsSettings();
        configuration.GetSection("ExternalSystems").Bind(settings);
        var options = Options.Create(settings);

        var resolver = new ExternalUrlResolver(options);

        // Act
        var result = resolver.GetAbsoluteUrl("System1", "Url1");

        // Assert
        Assert.Equal("https://api1.sys1.com/api/v1/url1", result);
    }

    [Fact]
    public void GetAbsoluteUrl_ReturnsExpectedUrl()
    {
        // Arrange
        var settings = new ExternalSystemsSettings
        {
            ApiDefinitions = new List<ApiDefinition>
            {
                new ApiDefinition
                {
                    Name = "System1",
                    BaseUrl = "https://api1.sys1.com",
                    RelativeUrls = new Dictionary<string, string>
                    {
                        { "Url1", "api/v1/url1" },
                        { "Url2", "api/v1/url2" }
                    }
                }
            }
        };

        var options = Options.Create(settings);
        var resolver = new ExternalUrlResolver(options);

        // Act
        var result = resolver.GetAbsoluteUrl("System1", "Url1");

        // Assert
        Assert.Equal("https://api1.sys1.com/api/v1/url1", result);
    }
}