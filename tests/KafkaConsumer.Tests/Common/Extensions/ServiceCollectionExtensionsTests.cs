using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Extensions;
using KafkaConsumer.Tests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KafkaConsumer.Tests.Extensions;

public class ServiceCollectionExtensionsTests : IClassFixture<HostFixture>
{
    private readonly IHost _host;
    public ServiceCollectionExtensionsTests(HostFixture fixture)
    {
        // Set up the host with logging
        _host = fixture.TestHost;
    }
    [Fact]
    public void RegisterEventHandlers_RegistersAllEventHandlerImplementations()
    {
        var handlers = _host.Services.GetServices<IEventHandler>().ToList();

        Assert.NotEmpty(handlers);
        //var n = nameof(handlers[1]);
        Assert.Contains(handlers, h => h.GetType().Name == "UpdateUserHandler");
        Assert.Contains(handlers, h => h.GetType().Name == "UpdateOrderHandler");
    }

    [Fact]
    public void AddOptionsConfigurations_RegistersConfigurationOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurationValues = new Dictionary<string, string?>
        {
            { "TopicConfigurations:CurrentSet", "Set1" },
            { "TopicConfigurations:Sets:Set1:0:TopicName", "topic_1" },
            { "TopicConfigurations:Sets:Set1:0:EventType", "user.created" },
            { "TopicConfigurations:Sets:Set1:0:HandlerName", "UpdateUser" },
            { "Kafka:BootstrapServers", "localhost:9092" },
            { "Kafka:GroupId", "test-group" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        // Act
        services.AddOptionsConfigurations(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var topicConfig = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<TopicSettings>>();
        var kafkaSettings = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<KafkaSettings>>();

        Assert.NotNull(topicConfig);
        Assert.NotNull(kafkaSettings);
        Assert.Equal("Set1", topicConfig.Value.CurrentSet);
        Assert.Equal("localhost:9092", kafkaSettings.Value.BootstrapServers);
    }
} 