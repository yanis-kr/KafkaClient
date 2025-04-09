using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KafkaConsumer.Tests.Common.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void RegisterEventHandlers_RegistersAllEventHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.RegisterEventHandlers();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var handlers = serviceProvider.GetServices<IEventHandler>();
        Assert.NotEmpty(handlers);
    }

    [Fact]
    public void AddOptionsConfigurations_ConfiguresAllOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["TopicConfigurations:CurrentSet"] = "Set1",
                ["TopicConfigurations:Sets:Set1:0:TopicName"] = "topic1",
                ["TopicConfigurations:Sets:Set1:0:EventType"] = "test.event",
                ["TopicConfigurations:Sets:Set1:0:HandlerName"] = "TestHandler",
                ["Kafka:BootstrapServers"] = "localhost:9092",
                ["Kafka:GroupId"] = "test-group"
            })
            .Build();

        // Act
        services.AddOptionsConfigurations(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var topicConfig = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TopicConfigurations>>();
        var kafkaSettings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<KafkaSettings>>();

        Assert.NotNull(topicConfig.Value);
        Assert.NotNull(kafkaSettings.Value);
        Assert.Equal("Set1", topicConfig.Value.CurrentSet);
        Assert.Single(topicConfig.Value.Sets["Set1"]);
        var topicEntry = topicConfig.Value.Sets["Set1"][0];
        Assert.Equal("topic1", topicEntry.TopicName);
        Assert.Equal("test.event", topicEntry.EventType);
        Assert.Equal("TestHandler", topicEntry.HandlerName);
        Assert.Equal("localhost:9092", kafkaSettings.Value.BootstrapServers);
        Assert.Equal("test-group", kafkaSettings.Value.GroupId);
    }
} 